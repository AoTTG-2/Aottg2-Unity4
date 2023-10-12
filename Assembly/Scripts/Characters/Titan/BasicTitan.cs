﻿using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Utility;
using Controllers;
using CustomSkins;
using System.Collections.Generic;
using SimpleJSONFixed;
using System.Collections;
using Effects;
using UI;
using Settings;
using CustomLogic;

namespace Characters
{
    class BasicTitan : BaseTitan
    {
        public BasicTitanComponentCache BasicCache;
        protected BasicTitanAnimations BasicAnimations;
        public bool IsCrawler;
        protected string _runAnimation;
        public BasicTitanSetup Setup;
        public Quaternion _oldHeadRotation;
        public float BellyFlopTime = 5.5f;
        protected bool _leftArmDisabled;
        protected bool _rightArmDisabled;
        protected float _leftArmDisabledTimeLeft;
        protected float _rightArmDisabledTimeLeft;
        protected float ArmDisableTime = 12f;
        protected float _originalCapsuleValue;
        public int TargetViewId = -1;
        public int HeadPrefab;

        public override List<string> EmoteActions => new List<string>() { "Laugh", "Nod", "Shake", "Roar" };

        public void Init(bool ai, string team, JSONNode data, int headPrefab)
        {
            HeadPrefab = headPrefab;
            if (ai)
            {
                var controller = gameObject.AddComponent<BaseTitanAIController>();
                controller.Init(data);
                Name = data["Name"].Value;
                IsCrawler = data["IsCrawler"].AsBool;
            }
            else
                gameObject.AddComponent<BasicTitanPlayerController>();
            if (IsCrawler)
                _runAnimation = BasicAnimations.RunCrawler;
            else
            {
                int runAnimationType = 1;
                if (data != null  && data.HasKey("RunAnimation"))
                    runAnimationType = data["RunAnimation"].AsInt;
                if (runAnimationType == 0)
                    _runAnimation = BasicAnimations.Runs[UnityEngine.Random.Range(0, BasicAnimations.Runs.Length)];
                else
                    _runAnimation = BasicAnimations.Runs[runAnimationType - 1];
            }
            Cache.PhotonView.RPC("SetCrawlerRPC", PhotonTargets.AllBuffered, new object[] { IsCrawler });
            base.Init(ai, team, data);
        }

        protected override Dictionary<string, float> GetRootMotionAnimations()
        {
            return new Dictionary<string, float>() { {BasicAnimations.AttackBellyFlop, 1f },
                { BasicAnimations.AttackBellyFlopGetup, 1f }, { BasicAnimations.AttackPunch, 1f }, { BasicAnimations.AttackPunchCombo, 1f} };
        }

        protected override void SetSizeParticles(float size)
        {
            base.SetSizeParticles(size);
            foreach (ParticleSystem system in new ParticleSystem[] {BasicCache.ForearmSmokeL, BasicCache.ForearmSmokeR})
            {
                system.startSize *= size;
                system.startSpeed *= size;
            }
            BasicCache.ForearmSmokeL.transform.localScale = Vector3.one * Size;
            BasicCache.ForearmSmokeR.transform.localScale = Vector3.one * Size;
        }

        [RPC]
        public void SetCrawlerRPC(bool crawler, PhotonMessageInfo info)
        {
            if (info.sender == Cache.PhotonView.owner)
            {
                IsCrawler = crawler;
                var capsule = (CapsuleCollider)BasicCache.Movebox;
                if (crawler)
                {
                    capsule.direction = 2;
                    capsule.radius *= 0.5f;
                    capsule.center = new Vector3(0f, capsule.radius, 0f);
                    _originalCapsuleValue = capsule.height;
                }
                else
                    _originalCapsuleValue = capsule.radius;
            }
        }

        protected override void Start()
        {
            _inGameManager.Titans.Add(this);
            base.Start();
            if (IsMine())
            {
                string setup = Setup.CreateRandomSetupJson(HeadPrefab);
                Cache.PhotonView.RPC("SetupRPC", PhotonTargets.AllBuffered, new object[] { setup });
                EffectSpawner.Spawn(EffectPrefabs.TitanSpawn, Cache.Transform.position, Quaternion.Euler(-90f, 0f, 0f), GetSpawnEffectSize());
            }
        }

        protected override BaseMovementSync CreateMovementSync()
        {
            return gameObject.AddComponent<BasicTitanMovementSync>();
        }

        [RPC]
        public void SetupRPC(string json, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Setup.Load(json);
        }

        protected override void CreateCache(BaseComponentCache cache)
        {
            BasicCache = new BasicTitanComponentCache(gameObject);
            base.CreateCache(BasicCache);
        }

        protected override void CreateAnimations(BaseTitanAnimations animations)
        {
            BasicAnimations = new BasicTitanAnimations();
            base.CreateAnimations(BasicAnimations);
        }

        public override void Emote(string emote)
        {
            if (CanAction())
            {
                string anim = string.Empty;
                if (emote == "Laugh")
                    anim = BasicAnimations.EmoteLaugh;
                else if (emote == "Nod")
                    anim = BasicAnimations.EmoteNod;
                else if (emote == "Shake")
                    anim = BasicAnimations.EmoteShake;
                else if (emote == "Roar")
                    anim = BasicAnimations.EmoteRoar;
                StateAction(TitanState.Emote, anim);
            }
        }

        public override void DisableArm(bool left)
        {
            if (State == TitanState.Attack || !AI)
                return;
            if (left && !_leftArmDisabled)
            {
                Cache.PhotonView.RPC("DisableArmRPC", PhotonTargets.All, new object[] { left });
                if (HoldHuman != null && HoldHumanLeft)
                {
                    Ungrab();
                    IdleWait(0.5f);
                }
            }
            else if (!left && !_rightArmDisabled)
            {
                Cache.PhotonView.RPC("DisableArmRPC", PhotonTargets.All, new object[] { left });
                if (HoldHuman != null && !HoldHumanLeft)
                {
                    Ungrab();
                    IdleWait(0.5f);
                }
            }
        }

        public override bool CanAttack()
        {
            return base.CanAttack() && !_leftArmDisabled && !_rightArmDisabled;
        }

        [RPC]
        public virtual void DisableArmRPC(bool left, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            if (left)
            {
                BasicCache.ForearmBloodL.Play(true);
                _leftArmDisabledTimeLeft = ArmDisableTime;
                _leftArmDisabled = true;
            }
            else
            {
                BasicCache.ForearmBloodR.Play(true);
                _rightArmDisabledTimeLeft = ArmDisableTime;
                _rightArmDisabled = true;
            }
        }

        public void Laugh(BaseCharacter character)
        {
            Cache.PhotonView.RPC("LaughRPC", Cache.PhotonView.owner, new object[] { character.Cache.Transform.position });
        }

        public void Distract(BaseCharacter character)
        {
            Cache.PhotonView.RPC("DistractRPC", Cache.PhotonView.owner, new object[] { character.Cache.PhotonView.viewID });
        }

        [RPC]
        public virtual void LaughRPC(Vector3 source)
        {
            if (!AI || !Cache.PhotonView.isMine)
                return;
            if (Vector3.Angle(Cache.Transform.forward, (source - Cache.Transform.position).normalized) < 80f)
                Emote("Laugh");
        }

        [RPC]
        public virtual void DistractRPC(int viewId)
        {
            if (!AI || !Cache.PhotonView.isMine)
                return;
            var character = Util.FindCharacterByViewId(viewId);
            GetComponent<BaseTitanAIController>().SetEnemy(character, 10f);
        }

        protected override void UpdateDisableArm()
        {
            if (_leftArmDisabled)
            {
                _leftArmDisabledTimeLeft -= Time.deltaTime;
                if (ArmDisableTime - _leftArmDisabledTimeLeft > 2.5f && !BasicCache.ForearmSmokeL.isPlaying)
                    BasicCache.ForearmSmokeL.Play();
                if (_leftArmDisabledTimeLeft <= 0f)
                {
                    _leftArmDisabled = false;
                    BasicCache.ForearmSmokeL.Stop();
                }
            }
            if (_rightArmDisabled)
            {
                _rightArmDisabledTimeLeft -= Time.deltaTime;
                if (ArmDisableTime - _rightArmDisabledTimeLeft > 2.5f && !BasicCache.ForearmSmokeR.isPlaying)
                    BasicCache.ForearmSmokeR.Play();
                if (_rightArmDisabledTimeLeft <= 0f)
                {
                    _rightArmDisabled = false;
                    BasicCache.ForearmSmokeR.Stop();
                }
            }
        }

        public override void Run()
        {
            _stepPhase = 0;
            StateActionWithTime(TitanState.Run, _runAnimation, 0f, 0.5f);
            if (IsCrawler && !BasicCache.BodyHitbox.IsActive())
                BasicCache.BodyHitbox.Activate();
        }

        public override void Jump(Vector3 direction)
        {
            _jumpDirection = direction;
            if (IsCrawler)
            {
                StateAction(TitanState.PreJump, BasicAnimations.JumpCrawler);
            }
            else
            {
                float stateTime = Cache.Animation[BasicAnimations.Jump].length / 2f;
                StateActionWithTime(TitanState.PreJump, BaseTitanAnimations.Jump, stateTime, 0.1f);
            }
        }

        public virtual void StunDirectional(bool left)
        {
            if (CanStun())
            {
                if (left)
                    StateActionWithTime(TitanState.Stun, BasicAnimations.StunLeft, StunTime, 0.1f);
                else
                    StateActionWithTime(TitanState.Stun, BasicAnimations.StunRight, StunTime, 0.1f);
            }
        }

        public override void StartJump()
        {
            base.StartJump();
            BasicCache.MouthHitbox.Activate();
        }

        public override void Eat()
        {
            if (HoldHuman == null)
                return;
            if (HoldHumanLeft)
                StateAction(TitanState.Eat, BasicAnimations.AttackEatL);
            else
                StateAction(TitanState.Eat, BasicAnimations.AttackEatR);
            NormalGrunt();
        }

        public override void Land()
        {
            if (IsCrawler)
                StateAction(TitanState.Land, BasicAnimations.LandCrawler);
            else
                StateAction(TitanState.Land, BaseTitanAnimations.Land);
            EffectSpawner.Spawn(EffectPrefabs.Boom2, Cache.Transform.position + Vector3.down * _currentGroundDistance,
                            Quaternion.Euler(270f, 0f, 0f), Size * SizeMultiplier);
        }

        public override void Fall()
        {
            if (IsCrawler)
                StateActionWithTime(TitanState.Fall, BasicAnimations.FallCrawler, 0f, 0.1f);
            else
                StateActionWithTime(TitanState.Fall, BaseTitanAnimations.Fall, 0f, 0.1f);
        }

        public override void Idle(float fadeTime)
        {
            if (IsCrawler)
                StateActionWithTime(TitanState.Idle, BasicAnimations.IdleCrawler, 0f, fadeTime);
            else
                StateActionWithTime(TitanState.Idle, BasicAnimations.Idle, 0f, fadeTime);
        }

        public override void Turn(Vector3 targetDirection)
        {
            if (!CanAction())
                return;
            float angle = GetAngleToTarget(targetDirection);
            string animation;
            if (IsCrawler)
            {
                if (angle > 0f)
                    animation = BasicAnimations.Turn90RCrawler;
                else
                    animation = BasicAnimations.Turn90LCrawler;
            }
            else
            {
                if (angle > 0f)
                    animation = BaseTitanAnimations.Turn90R;
                else
                    animation = BaseTitanAnimations.Turn90L;
            }
            targetDirection = Vector3.RotateTowards(Cache.Transform.forward, targetDirection, 120f * Mathf.Deg2Rad, float.MaxValue);
            _turnStartRotation = Cache.Transform.rotation;
            _turnTargetRotation = Quaternion.LookRotation(targetDirection);
            _currentTurnTime = 0f;
            _maxTurnTime = Cache.Animation[animation].length * 0.71f / Cache.Animation[animation].speed;
            StateActionWithTime(TitanState.Turn, animation, _maxTurnTime, 0.1f);
        }

        protected override IEnumerator WaitAndDie()
        {
            string dieAnimation = BasicAnimations.DieFront;
            if (State == TitanState.Stun)
                dieAnimation = BasicAnimations.DieBack;
            if (IsCrawler)
                dieAnimation = BasicAnimations.DieCrawler;
            else if (_currentStateAnimation == BasicAnimations.AttackBellyFlop || _currentStateAnimation == BasicAnimations.AttackBellyFlopGetup)
                dieAnimation = BasicAnimations.DieGround;
            else if (_currentStateAnimation == BasicAnimations.SitFall || _currentStateAnimation == BasicAnimations.SitIdle 
                || _currentStateAnimation == BasicAnimations.SitBlind)
                dieAnimation = BasicAnimations.DieSit;
            StateActionWithTime(TitanState.Dead, dieAnimation, 0f, 0.1f);
            yield return new WaitForSeconds(2f);
            EffectSpawner.Spawn(EffectPrefabs.TitanDie1, BaseTitanCache.Hip.position, Quaternion.Euler(-90f, 0f, 0f), GetSpawnEffectSize(), false);
            yield return new WaitForSeconds(3f);
            EffectSpawner.Spawn(EffectPrefabs.TitanDie2, BaseTitanCache.Hip.position, Quaternion.Euler(-90f, 0f, 0f), GetSpawnEffectSize(), false);
            PhotonNetwork.Destroy(gameObject);
        }

        protected override void Awake()
        {
            base.Awake();
            Setup = gameObject.AddComponent<BasicTitanSetup>();
            Cache.Animation[BasicAnimations.Jump].speed = 2f;
        }

        [RPC]
        public override void GetHitRPC(int viewId, string name, int damage, string type, string collider)
        {
            if (Dead)
                return;
            var settings = SettingsManager.InGameCurrent.Titan;
            if (type == "CannonBall")
            {
                base.GetHitRPC(viewId, name, damage, type, collider);
                return;
            }
            if (settings.TitanArmorEnabled.Value && (!IsCrawler || settings.TitanArmorCrawlerEnabled.Value))
            {
                if (damage < settings.TitanArmor.Value)
                    damage = 0;
            }
            if (type == "Stun")
            {
                if (!IsCrawler)
                {
                    var killer = Util.FindCharacterByViewId(viewId);
                    if (killer != null)
                    {
                        Vector3 direction = killer.Cache.Transform.position - Cache.Transform.position;
                        direction.y = 0f;
                        Cache.Transform.forward = direction.normalized;
                        Vector3 local = Cache.Transform.InverseTransformPoint(killer.Cache.Transform.position);
                        if (local.x < 0f)
                            StunDirectional(true);
                        else
                            StunDirectional(false);
                    }
                    else
                        StunDirectional(true);
                }
                base.GetHitRPC(viewId, name, damage, type, collider);
            }
            else if (BaseTitanCache.EyesHurtbox != null && collider == BaseTitanCache.EyesHurtbox.name && !IsCrawler)
                Blind();
            else if (BaseTitanCache.LegLHurtbox != null && !IsCrawler && (collider == BaseTitanCache.LegLHurtbox.name || collider == BaseTitanCache.LegRHurtbox.name))
                Cripple();
            else if (collider == BasicCache.ForearmLHurtbox.name && !IsCrawler)
                DisableArm(true);
            else if (collider == BasicCache.ForearmRHurtbox.name && !IsCrawler)
                DisableArm(false);
            else if (collider == BaseTitanCache.NapeHurtbox.name)
                base.GetHitRPC(viewId, name, damage, type, collider);
        }

        public override void Kick()
        {
            Attack(BasicTitanAttacks.AttackKick);
        }

        public override void Attack(string attack)
        {
            ResetAttackState(attack);
            if (_currentAttack == BasicTitanAttacks.AttackPunchCombo)
                StateAction(TitanState.Attack, BasicAnimations.AttackPunchCombo);
            else if (_currentAttack == BasicTitanAttacks.AttackPunch)
                StateAction(TitanState.Attack, BasicAnimations.AttackPunch);
            else if (_currentAttack == BasicTitanAttacks.AttackSlam)
                StateAction(TitanState.Attack, BasicAnimations.AttackSlam);
            else if (_currentAttack == BasicTitanAttacks.AttackBellyFlop)
                StateActionWithTime(TitanState.Attack, BasicAnimations.AttackBellyFlop, BellyFlopTime, 0.1f);
            else if (_currentAttack == BasicTitanAttacks.AttackKick)
                StateAction(TitanState.Attack, BasicAnimations.AttackKick);
            else if (_currentAttack == BasicTitanAttacks.AttackStomp)
                StateAction(TitanState.Attack, BasicAnimations.AttackStomp);
            else if (_currentAttack == BasicTitanAttacks.AttackBite)
            {
                string animation = AttackBite();
                StateAction(TitanState.Attack, animation);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackGrab)
            {
                DeactivateAllHitboxes();
                string animation = AttackGrab();
                StateAction(TitanState.Attack, animation, deactivateHitboxes: false);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackSlap)
            {
                string animation = AttackSlap();
                StateAction(TitanState.Attack, animation);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackBrush)
            {
                DeactivateAllHitboxes();
                string animation = AttackBrush();
                StateAction(TitanState.Attack, animation, deactivateHitboxes: false);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackSlapFace)
                StateAction(TitanState.Attack, BasicAnimations.AttackSlapFace);
            else if (_currentAttack == BasicTitanAttacks.AttackSlapBack)
                StateAction(TitanState.Attack, BasicAnimations.AttackSlapBack);
            else if (_currentAttack == BasicTitanAttacks.AttackSwing)
            {
                string animation = AttackSwing();
                StateAction(TitanState.Attack, animation);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackJump)
            {
                if (TargetEnemy != null)
                {
                    Vector3 to = TargetEnemy.Cache.Transform.position - BasicCache.Head.position;
                    float time = to.magnitude / JumpForce;
                    float down = 0.5f * Gravity.magnitude * time * time;
                    to.y += down;
                    Jump(to.normalized);
                }
                else
                    Jump(Vector3.up);
            }
            else if (_currentAttack == BasicTitanAttacks.AttackCrawlerJump)
            {
                if (TargetEnemy != null)
                {
                    Vector3 to = TargetEnemy.Cache.Transform.position - BasicCache.Head.position;
                    float time = to.magnitude / JumpForce;
                    float down = 0.5f * Gravity.magnitude * time * time;
                    to.y += down;
                    Jump(to.normalized);
                }
                else
                    Jump(Cache.Transform.forward + Vector3.up);
            }
        }

        protected string AttackBite()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            if (angleX > 45f)
                return BasicAnimations.AttackBiteR;
            else if (angleX < -45f)
                return BasicAnimations.AttackBiteL;
            else
                return BasicAnimations.AttackBiteF;
        }

        protected string AttackSwing()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            if (angleX > 0f)
            {
                return BasicAnimations.AttackSwingL;
            }
            else
            {
                return BasicAnimations.AttackSwingR;
            }
        }

        protected string AttackGrab()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            float distanceY = 0f;
            float distanceZ = 0f;
            if (TargetEnemy == null)
            {
                BasicCache.HandRHitbox.Activate(0.96f, 0.13f);
                return BasicAnimations.AttackGrabHeadBackL;
            }
            else
            {
                Vector3 diff = Cache.Transform.InverseTransformPoint(TargetEnemy.Cache.Transform.position);
                distanceY = diff.y;
                distanceZ = diff.z;
            }
            string grabChoice = "Ground";
            if (Mathf.Abs(distanceZ) <= 4f)
            {
                if (distanceY < 2f)
                    grabChoice = "Core";
                else if (distanceY < 11f)
                    grabChoice = "Stomach";
                else if (distanceY < 20f)
                    grabChoice = "Head";
                else
                    grabChoice = "High";
            }
            else if (Mathf.Abs(distanceZ) <= 8f)
            {
                if (distanceY < 5f)
                    grabChoice = "Ground";
                else if (distanceY <= 11f)
                    grabChoice = "Air";
                else if (distanceY < 20f)
                    grabChoice = "Head";
                else
                    grabChoice = "High";
            }
            else
            {
                if (distanceY < 5f)
                    grabChoice = "Ground";
                else if (distanceY < 20f)
                    grabChoice = "AirFar";
                else
                    grabChoice = "High";
            }
            if (grabChoice == "Core")
            {
                if (angleX > 0f)
                {
                    BasicCache.HandRHitbox.Activate(0.65f, 0.23f);
                    return BasicAnimations.AttackGrabCoreR;
                }
                else
                {
                    BasicCache.HandLHitbox.Activate(0.65f, 0.23f);
                    return BasicAnimations.AttackGrabCoreL;
                }
            }
            else if (grabChoice == "Stomach")
            {
                if (angleX > 0f)
                {
                    if (angleX > 90f)
                    {
                        BasicCache.HandRHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackR;
                    }
                    else
                    {
                        BasicCache.HandRHitbox.Activate(0.71f, 0.36f);
                        return BasicAnimations.AttackGrabStomachR;
                    }
                }
                else
                {
                    if (angleX < -90f)
                    {
                        BasicCache.HandLHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackL;
                    }
                    else
                    {
                        BasicCache.HandLHitbox.Activate(0.71f, 0.36f);
                        return BasicAnimations.AttackGrabStomachL;
                    }
                }
            }
            else if (grabChoice == "Head")
            {
                if (angleX > 0f)
                {
                    if (angleX > 90f)
                    {
                        BasicCache.HandLHitbox.Activate(0.96f, 0.13f);
                        return BasicAnimations.AttackGrabHeadBackR;
                    }
                    else
                    {
                        BasicCache.HandRHitbox.Activate(1.03f, 0.5f);
                        return BasicAnimations.AttackGrabHeadFrontR;
                    }
                }
                else
                {
                    if (angleX < -90f)
                    {
                        BasicCache.HandRHitbox.Activate(0.96f, 0.13f);
                        return BasicAnimations.AttackGrabHeadBackL;
                    }
                    else
                    {
                        BasicCache.HandLHitbox.Activate(1.03f, 0.5f);
                        return BasicAnimations.AttackGrabHeadFrontL;
                    }
                }
            }
            else if (grabChoice == "Ground")
            {
                if (angleX > 0f)
                {
                    BasicCache.HandRHitbox.Activate(0.91f, 0.35f);
                    if (angleX > 90f)
                        return BasicAnimations.AttackGrabGroundBackR;
                    else
                        return BasicAnimations.AttackGrabGroundFrontR;
                }
                else
                {
                    BasicCache.HandLHitbox.Activate(0.91f, 0.35f);
                    if (angleX < -90f)
                        return BasicAnimations.AttackGrabGroundBackL;
                    else
                        return BasicAnimations.AttackGrabGroundFrontL;
                }
            }
            else if (grabChoice == "Air")
            {
                if (angleX > 0f)
                {
                    if (angleX > 90f)
                    {
                        BasicCache.HandRHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackR;
                    }
                    else
                    {
                        BasicCache.HandRHitbox.Activate(0.4f, 0.3f);
                        return BasicAnimations.AttackGrabAirR;
                    }
                }
                else
                {
                    if (angleX < -90f)
                    {
                        BasicCache.HandLHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackL;
                    }
                    else
                    {
                        BasicCache.HandLHitbox.Activate(0.4f, 0.3f);
                        return BasicAnimations.AttackGrabAirL;
                    }
                }
            }
            else if (grabChoice == "AirFar")
            {
                if (angleX > 0f)
                {
                    if (angleX > 90f)
                    {
                        BasicCache.HandRHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackR;
                    }
                    else
                    {
                        BasicCache.HandRHitbox.Activate(0.76f, 0.27f);
                        return BasicAnimations.AttackGrabAirFarR;
                    }
                }
                else
                {
                    if (angleX < -90f)
                    {
                        BasicCache.HandLHitbox.Activate(0.88f, 0.36f);
                        return BasicAnimations.AttackGrabBackL;
                    }
                    else
                    {
                        BasicCache.HandLHitbox.Activate(0.76f, 0.27f);
                        return BasicAnimations.AttackGrabAirFarL;
                    }
                }
            }
            else if (grabChoice == "High")
            {
                if (angleX > 0f)
                {
                    BasicCache.HandRHitbox.Activate(0.88f, 0.43f);
                    return BasicAnimations.AttackGrabHighR;
                }
                else
                {
                    BasicCache.HandLHitbox.Activate(0.88f, 0.43f);
                    return BasicAnimations.AttackGrabHighL;
                }
            }
            return "";
        }

        protected string AttackSlap()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            float angleY = angles[1];
            bool left = angleX < 0f;
            if (angleY > 45f)
                return left ? BasicAnimations.AttackSlapHighL : BasicAnimations.AttackSlapHighR;
            else if (angleY > -10f)
                return left ? BasicAnimations.AttackSlapL : BasicAnimations.AttackSlapR;
            else
                return left ? BasicAnimations.AttackSlapLowL : BasicAnimations.AttackSlapLowR;
        }

        protected string AttackBrush()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            bool left = angleX > 0f;
            if (left)
            {
                BasicCache.HandLHitbox.Activate(0.5f, 0.5f);
                return BasicAnimations.AttackBrushChestL;
            }
            else
            {
                BasicCache.HandRHitbox.Activate(0.5f, 0.5f);
                return BasicAnimations.AttackBrushChestR;
            }
        }

        protected override void UpdateAttack()
        {
            float animationTime = GetAnimationTime();
            var rotation = Quaternion.Euler(270f, 0f, 0f);
            if (_currentAttack == BasicTitanAttacks.AttackPunchCombo)
            {
                if (_currentAttackStage == 0 && animationTime > 0.11f)
                {
                    PlaySound(TitanSounds.Swing1);
                    BasicCache.HandRHitbox.Activate(0f, 0.14f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.26f)
                {
                    PlaySound(TitanSounds.Swing2);
                    BasicCache.HandLHitbox.Activate(0f, 0.14f);
                    _currentAttackStage = 2;
                }
                else if (_currentAttackStage == 2 && animationTime > 0.495f)
                {
                    PlaySound(TitanSounds.Swing3);
                    BasicCache.HandLHitbox.Activate(0f, 0.15f);
                    BasicCache.HandRHitbox.Activate(0f, 0.15f);
                    _currentAttackStage = 3;
                }
                else if (_currentAttackStage == 3 && animationTime > 0.55f)
                {
                    var position = BasicCache.Transform.position + BasicCache.Transform.forward * 7f * Size;
                    EffectSpawner.Spawn(EffectPrefabs.Boom1, position, rotation, Size);
                    SpawnShatter(position);
                    _currentAttackStage = 4;
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackPunch)
            {
                if (_currentAttackStage == 0 && animationTime > 0.28f)
                {
                    PlaySound(TitanSounds.Swing1);
                    BasicCache.HandRHitbox.Activate(0f, 0.14f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.63f)
                {
                    PlaySound(TitanSounds.Swing2);
                    BasicCache.HandLHitbox.Activate(0f, 0.14f);
                    _currentAttackStage = 2;
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackSlam)
            {
                if (_currentAttackStage == 0 && animationTime > 0.42f)
                {
                    PlaySound(TitanSounds.Swing3);
                    BasicCache.HandLHitbox.Activate(0f, 0.15f);
                    BasicCache.HandRHitbox.Activate(0f, 0.15f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.46f)
                {
                    var position = BasicCache.Transform.position + BasicCache.Transform.forward * 7f * Size;
                    EffectSpawner.Spawn(EffectPrefabs.Boom1, position, rotation, Size);
                    SpawnShatter(position);
                    _currentAttackStage = 2;
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackBellyFlop)
            {
                if (_currentAttackStage == 0 && animationTime > 0.69f)
                {
                    _currentAttackStage = 1;
                    BasicCache.BodyHitbox.Activate(0f, 0.2f);
                }
                else if (_currentAttackStage == 1 && animationTime > 0.81f)
                {
                    _currentAttackStage = 2;
                    var position = Cache.Transform.position + Cache.Transform.forward * 5f;
                    EffectSpawner.Spawn(EffectPrefabs.Boom4, position, Quaternion.Euler(270f, Cache.Transform.rotation.eulerAngles.y, 0f), Size);
                }
                else if (_currentAttackStage == 2 && _stateTimeLeft < 1.1f)
                {
                    _currentAttackStage = 3;
                    CrossFade(BasicAnimations.AttackBellyFlopGetup, 0.1f);
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackSlap)
            {
                if (_currentAttackStage == 0 && animationTime > 0.335f)
                {
                    PlaySound(TitanSounds.Swing1);
                    if (_currentStateAnimation == BasicAnimations.AttackSlapL || _currentStateAnimation == BasicAnimations.AttackSlapHighL ||
                        _currentStateAnimation == BasicAnimations.AttackSlapLowL)
                        BasicCache.HandLHitbox.Activate(0f, 0.25f);
                    else
                        BasicCache.HandRHitbox.Activate(0f, 0.25f);
                    _currentAttackStage = 1;
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackKick)
            {
                if (_currentAttackStage == 0 && animationTime > 0.38f)
                {
                    BasicCache.FootLHitbox.Activate(0f, 0.25f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.43f)
                {
                    _currentAttackStage = 2;
                    var position = BasicCache.FootLHitbox.transform.position;
                    position.y = BasicCache.Transform.position.y;
                    EffectSpawner.Spawn(EffectPrefabs.Boom5, position, BasicCache.Transform.rotation, Size);
                    SpawnShatter(position);
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackStomp)
            {
                if (_currentAttackStage == 0 && animationTime > 0.385f)
                {
                    BasicCache.FootLHitbox.Activate(0f, 0.18f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.43f)
                {
                    _currentAttackStage = 2;
                    var position = BasicCache.FootLHitbox.transform.position;
                    position.y = BasicCache.Transform.position.y;
                    EffectSpawner.Spawn(EffectPrefabs.Boom2, position, rotation, Size);
                    SpawnShatter(position);
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackSwing)
            {
                if (_currentAttackStage == 0 && animationTime > 0.41f)
                {
                    PlaySound(TitanSounds.Swing1);
                    if (_currentStateAnimation == BasicAnimations.AttackSwingL)
                        BasicCache.HandLHitbox.Activate(0f, 0.13f);
                    else
                        BasicCache.HandRHitbox.Activate(0f, 0.13f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1 && animationTime > 0.46f)
                {
                    Vector3 position;
                    if (_currentStateAnimation == BasicAnimations.AttackSwingL)
                    {
                        position = BasicCache.HandLHitbox.transform.position;
                        position.y = BasicCache.Transform.position.y;
                    }
                    else
                    {
                        position = BasicCache.HandRHitbox.transform.position;
                        position.y = BasicCache.Transform.position.y;
                    }
                    EffectSpawner.Spawn(EffectPrefabs.Boom1, position, rotation, Size);
                    SpawnShatter(position);
                    _currentAttackStage = 2;
                }
            }
            else if (_currentAttack == BasicTitanAttacks.AttackBite)
            {
                float stage1Time = 0.55f;
                float stage2Time = 0.6f;
                if (_currentStateAnimation != BasicAnimations.AttackBiteF)
                {
                    stage1Time = 0.34f;
                    stage2Time = 0.41f;
                }
                if (_currentAttackStage == 0 && animationTime > stage1Time)
                {
                    BasicCache.MouthHitbox.Activate(0f, 0.15f);
                    _currentAttackStage = 1;
                }
                else if (_currentAttackStage == 1  && animationTime > stage2Time)
                {
                    var transform = BasicCache.MouthHitbox.transform;
                    var position = transform.position + transform.up * Size;
                    EffectSpawner.Spawn(EffectPrefabs.TitanBite, position, rotation, Size);
                    _currentAttackStage = 2;
                }
            }
            else if (_currentStateAnimation == BasicAnimations.AttackSlapBack)
            {
                if (_currentAttackStage == 0 && animationTime > 0.65f)
                {
                    _currentAttackStage = 1;
                    BasicCache.HandRHitbox.Activate(0f, 0.1f);
                }
                else if (_currentAttackStage == 1 && animationTime > 0.68f)
                {
                    _currentAttackStage = 2;
                    EffectSpawner.Spawn(EffectPrefabs.Boom3, BasicCache.HandRHitbox.transform.position, rotation, Size);
                }
            }
            else if (_currentStateAnimation == BasicAnimations.AttackSlapFace)
            {
                if (_currentAttackStage == 0 && animationTime > 0.64f)
                {
                    _currentAttackStage = 1;
                    BasicCache.HandRHitbox.Activate(0f, 0.3f);
                }
                else if (_currentAttackStage == 1 && animationTime > 0.68f)
                {
                    _currentAttackStage = 2;
                    EffectSpawner.Spawn(EffectPrefabs.Boom3, BasicCache.HandRHitbox.transform.position, rotation, Size);
                }
            }
        }

        protected void SpawnShatter(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 1f, Vector3.down, out hit, 2f, GroundMask.value))
            {
                EffectSpawner.Spawn(EffectPrefabs.GroundShatter, hit.point + Vector3.up * 0.1f, Quaternion.identity, Size);
            }
        }

        protected override void UpdateEat()
        {
            if (HoldHuman == null  && _stateTimeLeft > 4.72f)
            {
                IdleWait(1f);
                return;
            }
            if (_stateTimeLeft <= 4.72f)
            {
                if (HoldHuman != null)
                {
                    int damage = 100;
                    if (CustomDamageEnabled)
                        damage = CustomDamage;
                    HoldHuman.GetHit(this, damage, "Eat", "");
                    HoldHuman = null;
                }
            }
        }

        public override void OnHit(BaseHitbox hitbox, object victim, Collider collider, string type, bool firstHit)
        {
            int damage = 100;
            if (CustomDamageEnabled)
                damage = CustomDamage;
            if (victim is CustomLogicCollisionHandler)
            {
                ((CustomLogicCollisionHandler)victim).GetHit(this, Name, damage, type);
                return;
            }
            var victimChar = (BaseCharacter)victim;
            if (State == TitanState.Attack && _currentAttack == BasicTitanAttacks.AttackGrab && victim is Human)
            {
                var human = (Human)victim;
                if (HoldHuman == null && firstHit && !human.Dead)
                {
                    HoldHumanLeft = hitbox == BasicCache.HandLHitbox;
                    if (HoldHumanLeft)
                        human.GetHit(this, 0, "GrabLeft", collider.name);
                    else
                        human.GetHit(this, 0, "GrabRight", collider.name);
                }
            }
            else if (victim is BaseTitan)
            {
                if (firstHit)
                {
                    EffectSpawner.Spawn(EffectPrefabs.PunchHit, hitbox.transform.position, Quaternion.identity);
                    if (!victimChar.Dead)
                    {
                        if (IsMainCharacter())
                            ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                        victimChar.GetHit(this, damage, "Stun", collider.name);
                    }
                }
            }
            else
            {
                if (!victimChar.Dead)
                {
                    if (IsMainCharacter())
                        ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                    victimChar.GetHit(this, damage, "", collider.name);
                }
            }
        }

        protected void LateUpdateHead(BaseCharacter target)
        {
            if (target != null)
            {
                Vector3 targetPosition = target.Cache.Transform.position;
                if (target is BaseTitan)
                    targetPosition = ((BaseTitan)target).BaseTitanCache.Head.position;
                Vector3 vector = target.Cache.Transform.position - Cache.Transform.position;
                float angle = -Mathf.Atan2(vector.z, vector.x) * Mathf.Rad2Deg;
                float num = -Mathf.DeltaAngle(angle, Cache.Transform.rotation.eulerAngles.y - 90f);
                num = Mathf.Clamp(num, -40f, 40f);
                float y = (BasicCache.Neck.position.y + (Size * 2f)) - targetPosition.y;
                float distance = Util.DistanceIgnoreY(target.Cache.Transform.position, BasicCache.Transform.position);
                float num2 = Mathf.Atan2(y, distance) * Mathf.Rad2Deg;
                num2 = Mathf.Clamp(num2, -40f, 30f);
                BasicCache.Head.rotation = Quaternion.Euler(BasicCache.Head.rotation.eulerAngles.x + num2,
                    BasicCache.Head.rotation.eulerAngles.y + num, BasicCache.Head.rotation.eulerAngles.z);
                BasicCache.Head.localRotation = Quaternion.Lerp(_oldHeadRotation, BasicCache.Head.localRotation, Time.deltaTime * 10f);
            }
            else
                BasicCache.Head.localRotation = Quaternion.Lerp(_oldHeadRotation, BasicCache.Head.localRotation, Time.deltaTime * 10f);
            _oldHeadRotation = BasicCache.Head.localRotation;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (IsMine())
            {
                if (TargetEnemy != null && (State == TitanState.Idle || State == TitanState.Run || State == TitanState.Walk))
                {
                    TargetViewId = TargetEnemy.Cache.PhotonView.viewID;
                    LateUpdateHead(TargetEnemy);
                }
                else
                {
                    TargetViewId = -1;
                    LateUpdateHead(null);
                }
                if ((State == TitanState.Run || State == TitanState.Walk) && HasDirection)
                    Cache.Transform.rotation = Quaternion.Lerp(Cache.Transform.rotation, GetTargetRotation(), Time.deltaTime * RotateSpeed);
            }
            else
            {
                var character = Util.FindCharacterByViewId(TargetViewId);
                LateUpdateHead(character);
            }
            if (_leftArmDisabled)
            {
                BasicCache.ForearmL.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                BasicCache.ForearmL.localRotation = Quaternion.identity;
            }
            else
                BasicCache.ForearmL.localScale = Vector3.one;
            if (_rightArmDisabled)
            {
                BasicCache.ForearmR.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                BasicCache.ForearmR.localRotation = Quaternion.identity;
            }
            else
                BasicCache.ForearmR.localScale = Vector3.one;
            BasicCache.ForearmSmokeL.transform.position = BasicCache.ForearmL.position;
            BasicCache.ForearmSmokeR.transform.position = BasicCache.ForearmR.position;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (BasicCache.ForearmSmokeL != null)
                Destroy(BasicCache.ForearmSmokeL.gameObject);
            if (BasicCache.ForearmSmokeR != null)
                Destroy(BasicCache.ForearmSmokeR.gameObject);
        }

        protected override int GetFootstepPhase()
        {
            if (Cache.Animation.IsPlaying(BasicAnimations.Walk))
            {
                float time = Cache.Animation[BasicAnimations.Walk].normalizedTime % 1f;
                return (time >= 0.1f && time < 0.6f) ? 1 : 0;
            }
            string run = GetPlayingRunAnimation();
            if (run != "")
            {
                float time = Cache.Animation[run].normalizedTime % 1f;
                return (time >= 0f && time < 0.5f) ? 0 : 1;
            }
            return _stepPhase;
        }

        protected string GetPlayingRunAnimation()
        {
            if (Cache.Animation.IsPlaying(BasicAnimations.RunCrawler))
                return BasicAnimations.RunCrawler;
            foreach (string anim in BasicAnimations.Runs)
            {
                if (Cache.Animation.IsPlaying(anim))
                    return anim;
            }
            return "";
        }

        protected override void CheckGround()
        {
            var collider = (CapsuleCollider)(BaseTitanCache.Movebox);
            if (State == TitanState.Jump || State == TitanState.StartJump)
            {
                if (IsCrawler)
                    collider.height = _originalCapsuleValue * 0.7f;
                else
                    collider.radius = _originalCapsuleValue * 0.7f;
            }
            else
            {
                if (IsCrawler)
                    collider.height = _originalCapsuleValue;
                else
                    collider.radius = _originalCapsuleValue;
            }
            if (IsCrawler)
            {
                float radius = BaseTitanCache.Movebox.transform.lossyScale.x * collider.radius;
                float height = BaseTitanCache.Movebox.transform.lossyScale.x * collider.height - radius * 2f;
                float halfHeight = 0.5f * height;
                Vector3 position = Cache.Transform.position + Vector3.up * (radius + 1f);
                Vector3 start = position - Cache.Transform.forward * halfHeight;
                Vector3 end = position + Cache.Transform.forward * halfHeight;
                RaycastHit hit;
                if (Physics.CapsuleCast(start, end, radius, Vector3.down, out hit, 1f + GroundDistance, GroundMask.value))
                {
                    if (!Grounded)
                        Grounded = JustGrounded = true;
                    _currentGroundDistance = Mathf.Clamp(hit.distance - 1f, 0f, GroundDistance);
                }
                else
                {
                    Grounded = false;
                    _currentGroundDistance = GroundDistance;
                }
            }
            else
                base.CheckGround();
        }
    }
}

using ApplicationManagers;
using Controllers;
using CustomLogic;
using CustomSkins;
using Effects;
using GameProgress;
using Map;
using Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using Utility;
using Weather;

namespace Characters
{
    class Human : BaseCharacter
    {
        // setup
        public HumanComponentCache HumanCache;
        public BaseUseable Special;
        public BaseUseable Weapon;
        public HookUseable HookLeft;
        public HookUseable HookRight;
        public HumanMountState MountState = HumanMountState.None;
        public Horse Horse;
        public HumanSetup Setup;
        public bool FinishSetup;
        private HumanCustomSkinLoader _customSkinLoader;
        public override List<string> EmoteActions => new List<string>() { "Salute", "Dance", "Flip", "Wave1", "Wave2", "Eat" };
        public static LayerMask AimMask = PhysicsLayer.GetMask(PhysicsLayer.TitanPushbox, PhysicsLayer.MapObjectProjectiles,
           PhysicsLayer.MapObjectEntities, PhysicsLayer.MapObjectAll);

        // state
        private HumanState _state = HumanState.Idle;
        public float CurrentGas = -1f;
        public float MaxGas = -1f;
        private float GasUsage = 0.2f;
        public BaseTitan Grabber;
        public Transform GrabHand;
        public MapObject MountedMapObject;
        public Transform MountedTransform;
        public int AccelerationStat;
        public int RunSpeedStat;

        // physics
        public float ReelInAxis = 0f;
        public float ReelOutAxis = 0f;
        public float ReelOutScrollTimeLeft = 0f;
        public float TargetMagnitude = 0f;
        public bool IsWalk;
        private const float MaxVelocityChange = 10f;
        protected float StunTime = 1f;
        public float RunSpeed;
        private float _originalDashSpeed;
        public Quaternion _targetRotation;
        private float _wallRunTime = 0f;
        private bool _wallJump = false;
        private bool _launchLeft;
        private bool _launchRight;
        private float _launchLeftTime;
        private float _launchRightTime;
        private bool _needLean;
        private bool _almostSingleHook;
        private bool _leanLeft;
        private bool _interpolate;
        public override LayerMask GroundMask => PhysicsLayer.GetMask(PhysicsLayer.TitanPushbox, PhysicsLayer.MapObjectEntities,
            PhysicsLayer.MapObjectAll);

        // actions
        public string StandAnimation;
        public string AttackAnimation;
        public string RunAnimation;
        public bool _attackRelease;
        public bool _attackButtonRelease;
        private float _stateTimeLeft = 0f;
        private float _dashTimeLeft = 0f;
        private bool _cancelGasDisable;
        private bool _leftArmAim;
        private bool _rightArmAim;
        private bool _animationStopped;
        private Vector3 _gunTarget;
        private bool _needFinishReload;
        private string _reloadAnimation;
        private float _dashCooldownLeft = 0f;
        private Human _hookHuman;
        private bool _hookHumanLeft;
        private Dictionary<BaseTitan, float> _lastNapeHitTimes = new Dictionary<BaseTitan, float>();

        [RPC]
        public override void MarkDeadRPC(PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Dead = true;
            Setup.DeleteDie();
            if (IsMine())
                FalseAttack();
        }

        [RPC]
        public virtual void UngrabRPC(PhotonMessageInfo info)
        {
            if (Grabber == null || info.sender != Grabber.Cache.PhotonView.owner)
                return;
            Ungrab(false, true);
        }

        public Vector3 GetAimPoint()
        {
            RaycastHit hit;
            Ray ray = SceneLoader.CurrentCamera.Camera.ScreenPointToRay(Input.mousePosition);
            Vector3 target = ray.origin + ray.direction * 1000f;
            if (Physics.Raycast(ray, out hit, 1000f, AimMask.value))
                target = hit.point;
            return target;
        }

        public Vector3 GetAimPoint(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            Vector3 target = origin + direction * 1000f;
            if (Physics.Raycast(origin, direction, out hit, 1000f, AimMask.value))
                target = hit.point;
            return target;
        }

        public bool CanJump()
        {
            return (Grounded && (State == HumanState.Idle || State == HumanState.Slide) &&
                !Cache.Animation.IsPlaying(HumanAnimations.Jump) && !Cache.Animation.IsPlaying(HumanAnimations.HorseMount));
        }

        public void Jump()
        {
            Idle();
            CrossFade(HumanAnimations.Jump, 0.1f);
            PlaySound(HumanSounds.Jump);
            ToggleSparks(false);
        }

        public void Mount(Transform transform)
        {
            Unmount(true);
            MountState = HumanMountState.MapObject;
            MountedTransform = transform;
            SetInterpolation(false);
            GetComponent<CapsuleCollider>().isTrigger = true;
        }

        public void Mount(MapObject mapObject)
        {
            Mount(mapObject.GameObject.transform);
            MountedMapObject = mapObject;
        }

        public void Unmount(bool immediate)
        {
            SetInterpolation(true);
            if (MountState == HumanMountState.Horse && !immediate)
            {
                PlayAnimation(HumanAnimations.HorseDismount);
                Cache.Rigidbody.AddForce((((Vector3.up * 10f) - (Cache.Transform.forward * 2f)) - (Cache.Transform.right * 1f)), ForceMode.VelocityChange);
                MountState = HumanMountState.None;
            }
            else
            {
                MountState = HumanMountState.None;
                Idle();
                GetComponent<CapsuleCollider>().isTrigger = false;
            }
            MountedTransform = null;
            MountedMapObject = null;
        }

        public void MountHorse()
        {
            if (Horse != null && MountState == HumanMountState.None && Vector3.Distance(Horse.Cache.Transform.position, Cache.Transform.position) < 15f)
            {
                PlayAnimation(HumanAnimations.HorseMount);
                TargetAngle = Horse.transform.rotation.eulerAngles.y;
            }
        }

        public void Dodge(float targetAngle)
        {
            State = HumanState.GroundDodge;
            TargetAngle = targetAngle;
            _targetRotation = GetTargetRotation();
            CrossFade(HumanAnimations.Dodge, 0.1f);
            PlaySound(HumanSounds.Dodge);
            ToggleSparks(false);
        }

        public void DodgeWall()
        {
            State = HumanState.GroundDodge;
            PlayAnimation(HumanAnimations.Dodge, 0.2f);
            ToggleSparks(false);
        }

        public void Dash(float targetAngle)
        {
            if (_dashTimeLeft <= 0f && CurrentGas > 0 && MountState == HumanMountState.None &&
                State != HumanState.Grab && _dashCooldownLeft <= 0f)
            {
                UseGas(Mathf.Min(MaxGas * 0.04f, 10));
                TargetAngle = targetAngle;
                Vector3 direction = GetTargetDirection();
                _originalDashSpeed = Cache.Rigidbody.velocity.magnitude;
                _targetRotation = GetTargetRotation();
                Cache.Rigidbody.rotation = _targetRotation;
                EffectSpawner.Spawn(EffectPrefabs.GasBurst, Cache.Transform.position, Cache.Transform.rotation);
                PlaySound(HumanSounds.GasBurst);
                _dashTimeLeft = 0.5f;
                CrossFade(HumanAnimations.Dash, 0.1f, 0.1f);
                State = HumanState.AirDodge;
                FalseAttack();
                Cache.Rigidbody.AddForce(direction * 40f, ForceMode.VelocityChange);
                _dashCooldownLeft = 0.2f;
            }
        }

        public void Idle()
        {
            if (State == HumanState.Attack || State == HumanState.SpecialAttack)
                FalseAttack();
            State = HumanState.Idle;
            string animation = HumanAnimations.StandFemale;
            if (Setup.Weapon == HumanWeapon.Gun)
                animation = HumanAnimations.StandGun;
            else if (Setup.CustomSet.Sex.Value == (int)HumanSex.Male)
                animation = HumanAnimations.StandMale;
            CrossFade(animation, 0.1f);
        }

        public void Grab(BaseTitan grabber, Transform hand)
        {
            if (MountState != HumanMountState.None)
                Unmount(true);
            HookLeft.DisableActiveHook();
            HookRight.DisableActiveHook();
            UnhookHuman(true);
            UnhookHuman(false);
            State = HumanState.Grab;
            grabber.Cache.PhotonView.RPC("GrabRPC", grabber.Cache.PhotonView.owner, new object[] { Cache.PhotonView.viewID });
            GetComponent<CapsuleCollider>().isTrigger = true;
            FalseAttack();
            Grabber = grabber;
            GrabHand = hand;
            Cache.PhotonView.RPC("SetSmokeRPC", PhotonTargets.All, new object[] { false });
            PlayAnimation(HumanAnimations.Grabbed);
            ToggleSparks(false);
        }

        public void Ungrab(bool notifyTitan, bool idle)
        {
            if (notifyTitan && Grabber != null)
                Grabber.Cache.PhotonView.RPC("UngrabRPC", Grabber.Cache.PhotonView.owner, new object[0]);
            Grabber = null;
            GetComponent<CapsuleCollider>().isTrigger = false;
            if (idle)
                Idle();
        }

        public void SpecialActionState(float time)
        {
            State = HumanState.SpecialAction;
            _stateTimeLeft = time;
        }

        public void TransformShifter(string shifter, float liveTime)
        {
            _inGameManager.SpawnPlayerShifterAt(shifter, liveTime, Cache.Transform.position);
            PhotonNetwork.Destroy(gameObject);
        }

        public void Reload()
        {
            if (Setup.Weapon == HumanWeapon.Gun && !SettingsManager.InGameCurrent.Misc.GunsAirReload.Value && !Grounded)
                return;
            if (Weapon is AmmoWeapon)
            {
                if (((AmmoWeapon)Weapon).AmmoLeft <= 0)
                    return;
                if (Weapon is GunWeapon)
                {
                    Setup._part_blade_l.SetActive(false);
                    Setup._part_blade_r.SetActive(false);
                }
                else if (Weapon is ThunderspearWeapon)
                {
                    SetThunderspears(false, false);
                }
                PlaySound(HumanSounds.GunReload);
            }
            else if (Weapon is BladeWeapon)
            {
                if (((BladeWeapon)Weapon).BladesLeft <= 0)
                    return;
                Setup._part_blade_l.SetActive(false);
                Setup._part_blade_r.SetActive(false);
                if (Grounded)
                    PlaySound(HumanSounds.BladeReloadGround);
                else
                    PlaySound(HumanSounds.BladeReloadAir);
            }
            if (Setup.Weapon == HumanWeapon.Gun || Setup.Weapon == HumanWeapon.Thunderspear)
            {
                if (Grounded)
                    _reloadAnimation = "AHSS_gun_reload_both_air";
                else
                    _reloadAnimation = "AHSS_gun_reload_both";
            }
            else
            {
                if (Grounded)
                    _reloadAnimation = "changeBlade";
                else
                    _reloadAnimation = "changeBlade_air";
            }
            CrossFade(_reloadAnimation, 0.1f, 0f);
            State = HumanState.Reload;
            _stateTimeLeft = Cache.Animation[_reloadAnimation].length / Cache.Animation[_reloadAnimation].speed;
            _needFinishReload = true;
        }

        protected void FinishReload()
        {
            if (!_needFinishReload)
                return;
            _needFinishReload = false;
            Weapon.Reload();
            if (Weapon is BladeWeapon || Weapon is GunWeapon)
            {
                Setup._part_blade_l.SetActive(true);
                Setup._part_blade_r.SetActive(true);
            }
            else if (Weapon is ThunderspearWeapon)
                SetThunderspears(true, true);
        }

        public bool Refill()
        {
            if (!Grounded || State != HumanState.Idle)
                return false;
            State = HumanState.Refill;
            ToggleSparks(false);
            CrossFade(HumanAnimations.Refill, 0.1f);
            PlaySound(HumanSounds.Refill);
            _stateTimeLeft = Cache.Animation[HumanAnimations.Refill].length;
            return true;
        }

        public bool NeedRefill()
        {
            if (CurrentGas < MaxGas)
                return true;
            if (Weapon is BladeWeapon)
            {
                var weapon = (BladeWeapon)Weapon;
                return weapon.BladesLeft < weapon.MaxBlades || weapon.CurrentDurability < weapon.MaxDurability;
            }
            else if (Weapon is AmmoWeapon)
            {
                var weapon = (AmmoWeapon)Weapon;
                return weapon.NeedRefill();
            }
            return false;
        }

        public void FinishRefill()
        {
            if (Weapon == null || Dead)
                return;
            if (Weapon is BladeWeapon)
            {
                Setup._part_blade_l.SetActive(true);
                Setup._part_blade_r.SetActive(true);
            }
            Weapon.Reset();
            CurrentGas = MaxGas;
        }

        public override void Emote(string emote)
        {
            if (CanEmote())
            {
                if (State == HumanState.Attack)
                    FalseAttack();
                string animation = HumanAnimations.Salute;
                if (emote == "Salute")
                    animation = HumanAnimations.Salute;
                else if (emote == "Dance")
                    animation = HumanAnimations.SpecialArmin;
                else if (emote == "Flip")
                    animation = HumanAnimations.Dodge;
                else if (emote == "Wave1")
                    animation = HumanAnimations.SpecialMarco0;
                else if (emote == "Wave2")
                    animation = HumanAnimations.SpecialMarco1;
                else if (emote == "Eat")
                    animation = HumanAnimations.SpecialSasha;
                EmoteAnimation(animation);
                ToggleSparks(false);
            }
        }

        public void EmoteAnimation(string animation)
        {
            State = HumanState.EmoteAction;
            CrossFade(animation, 0.1f);
            _stateTimeLeft = Cache.Animation[animation].length;
            ToggleSparks(false);
        }

        public bool CanEmote()
        {
            return !Dead && State != HumanState.Grab && State != HumanState.AirDodge && State != HumanState.EmoteAction && State != HumanState.SpecialAttack && MountState == HumanMountState.None
                && State != HumanState.Stun;
        }

        public override Transform GetCameraAnchor()
        {
            return HumanCache.Head;
        }

        protected override void CreateCache(BaseComponentCache cache)
        {
            HumanCache = new HumanComponentCache(gameObject);
            base.CreateCache(HumanCache);
        }

        protected override IEnumerator WaitAndDie()
        {
            if (State == HumanState.Grab)
                PlaySound(HumanSounds.Death5);
            else if (Grounded)
                PlaySound(HumanSounds.Death2);
            else
                PlaySound(HumanSounds.Death2);
            EffectSpawner.Spawn(EffectPrefabs.Blood2, Cache.Transform.position, Cache.Transform.rotation);
            yield return new WaitForSeconds(2f);
            PhotonNetwork.Destroy(gameObject);
        }

        public void Init(bool ai, string team, InGameCharacterSettings settings)
        {
            base.Init(ai, team);
            Setup.Copy(settings);
            if (!ai)
                gameObject.AddComponent<HumanPlayerController>();
        }

        protected override void Awake()
        {
            base.Awake();
            HumanCache = (HumanComponentCache)Cache;
            Cache.Rigidbody.freezeRotation = true;
            Cache.Rigidbody.useGravity = false;
            Setup = gameObject.AddComponent<HumanSetup>();
            _customSkinLoader = gameObject.AddComponent<HumanCustomSkinLoader>();
            Destroy(gameObject.GetComponent<SmoothSyncMovement>());
            CustomAnimationSpeed();
        }

        protected override void Start()
        {
            _inGameManager.Humans.Add(this);
            base.Start();
            if (IsMine())
            {
                SetInterpolation(true);
                Cache.PhotonView.RPC("SetupRPC", PhotonTargets.AllBuffered, new object[] { Setup.CustomSet.SerializeToJsonString(), (int)Setup.Weapon });
                LoadSkin();
                if (SettingsManager.InGameCurrent.Misc.Horses.Value)
                {
                    Horse = (Horse)CharacterSpawner.Spawn(CharacterPrefabs.Horse, Cache.Transform.position + Vector3.right * 2f, Quaternion.identity);
                    Horse.Init(this);
                }
                if (DebugTesting.DebugPhase)
                    GetComponent<CapsuleCollider>().isTrigger = true;
            }
            else
            {
            }
        }

        public override void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            base.OnPhotonPlayerConnected(player);
            if (IsMine())
            {
                Cache.PhotonView.RPC("SetInterpolationRPC", player, new object[] { _interpolate });
            }
        }

        [RPC]
        public override void GetHitRPC(int viewId, string name, int damage, string type, string collider)
        {
            if (Dead)
                return;
            if (type == "Eat")
            {
                base.GetHitRPC(viewId, name, damage, type, collider);
                if (!Dead)
                    Ungrab(false, true);
            }
            else if (type.StartsWith("Grab"))
            {
                if (State == HumanState.Grab)
                    return;
                var titan = (BaseTitan)Util.FindCharacterByViewId(viewId);
                if (type == "GrabLeft")
                    Grab(titan, titan.BaseTitanCache.GrabLSocket);
                else
                    Grab(titan, titan.BaseTitanCache.GrabRSocket);
            }
            else
                base.GetHitRPC(viewId, name, damage, type, collider);
        }

        public override void OnHit(BaseHitbox hitbox, BaseCharacter victim, Collider collider, string type, bool firstHit)
        {
            if (hitbox != null)
            {
                if (hitbox == HumanCache.BladeHitLeft || hitbox == HumanCache.BladeHitRight)
                    type = "Blade";
                else if (hitbox == HumanCache.GunHit)
                    type = "Gun";
            }
            int damage = Mathf.Max((int)(Cache.Rigidbody.velocity.magnitude * 10f), 10);
            if (type == "Blade")
            {
                EffectSpawner.Spawn(EffectPrefabs.Blood1, hitbox.transform.position, Quaternion.Euler(270f, 0f, 0f));
                PlaySound(HumanSounds.BladeHit);
                var weapon = (BladeWeapon)Weapon;
                weapon.UseDurability(2f);
                if (weapon.CurrentDurability == 0f)
                {
                    Setup._part_blade_l.SetActive(false);
                    Setup._part_blade_r.SetActive(false);
                    PlaySound(HumanSounds.BladeBreak);
                }
                damage = (int)(damage * CharacterData.HumanWeaponInfo["Blade"]["DamageMultiplier"].AsFloat);
            }
            else if (type == "Gun")
                damage = (int)(damage * CharacterData.HumanWeaponInfo["Gun"]["DamageMultiplier"].AsFloat);
            if (CustomDamageEnabled)
                damage = CustomDamage;
            if (!victim.Dead)
            {
                if (victim is BaseTitan)
                {
                    var titan = (BaseTitan)victim;
                    if (titan.BaseTitanCache.NapeHurtbox == collider)
                    {
                        if (type == "Blade" && !CheckTitanNapeAngle(hitbox.transform.position, titan.BaseTitanCache.Head.transform,
                            CharacterData.HumanWeaponInfo["Blade"]["RestrictAngle"].AsFloat))
                            return;
                        if (type == "Gun" && !CheckTitanNapeAngle(hitbox.transform.position, titan.BaseTitanCache.Head.transform,
                            CharacterData.HumanWeaponInfo["Gun"]["RestrictAngle"].AsFloat))
                            return;
                        if (_lastNapeHitTimes.ContainsKey(titan) && (_lastNapeHitTimes[titan] + 0.2f) > Time.time)
                            return;
                        ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                        if (type == "Blade" && SettingsManager.GraphicsSettings.BloodSplatterEnabled.Value)
                            ((InGameMenu)UIManager.CurrentMenu).ShowBlood();
                        if (type == "Blade")
                            PlaySound(HumanSounds.BladeHitNape);
                        _lastNapeHitTimes[titan] = Time.time;
                    }
                    if (titan.BaseTitanCache.Hurtboxes.Contains(collider))
                    {
                        if (collider != titan.BaseTitanCache.NapeHurtbox && titan is BasicTitan && ((BasicTitan)titan).IsCrawler)
                            return;
                        EffectSpawner.Spawn(EffectPrefabs.CriticalHit, hitbox.transform.position, Quaternion.Euler(270f, 0f, 0f));
                        victim.GetHit(this, damage, type, collider.name);
                    }
                }
                else
                    victim.GetHit(this, damage, type, collider.name);
            }
        }

        bool CheckTitanNapeAngle(Vector3 position, Transform nape, float angle)
        {
            Vector3 direction = (position - nape.position).normalized;
            return Vector3.Angle(-nape.forward, direction) < angle;
        }

        protected void Update()
        {
            if (IsMine() && !Dead)
            {
                _stateTimeLeft -= Time.deltaTime;
                _dashCooldownLeft -= Time.deltaTime;
                UpdateBladeTrails();
                if (State == HumanState.Grab)
                {
                    if (Grabber == null || Grabber.Dead)
                        Ungrab(false, true);
                    else
                    {
                        Cache.Transform.position = GrabHand.transform.position;
                        Cache.Transform.rotation = GrabHand.transform.rotation;
                    }
                }
                else if (MountState == HumanMountState.MapObject)
                {
                    if (MountedTransform == null)
                        Unmount(true);
                    else
                    {
                        Cache.Transform.position = MountedTransform.position;
                        Cache.Transform.rotation = MountedTransform.rotation;
                    }
                }
                else if (MountState == HumanMountState.Horse)
                {
                    if (Horse == null)
                        Unmount(true);
                    else
                    {
                        Cache.Transform.position = Horse.Cache.Transform.position + Vector3.up * 1.68f;
                        Cache.Transform.rotation = Horse.Cache.Transform.rotation;
                    }
                }
                else if (State == HumanState.Attack)
                {
                    if (Setup.Weapon == HumanWeapon.Blade)
                    {
                        var bladeWeapon = (BladeWeapon)Weapon;
                        if (!bladeWeapon.IsActive)
                            _attackButtonRelease = true;
                        if (!_attackRelease)
                        {
                            if (_attackButtonRelease)
                            {
                                ContinueAnimation();
                                _attackRelease = true;
                            }
                            else if (Cache.Animation[AttackAnimation].normalizedTime >= 0.32f)
                                PauseAnimation();
                        }
                        float startTime;
                        float endTime;
                        if (bladeWeapon.CurrentDurability <= 0f)
                            startTime = endTime = -1f;
                        else if (AttackAnimation == "attack4")
                        {
                            startTime = 0.6f;
                            endTime = 0.9f;
                        }
                        else
                        {
                            startTime = 0.5f;
                            endTime = 0.85f;
                        }
                        if (Cache.Animation[AttackAnimation].normalizedTime > startTime && Cache.Animation[AttackAnimation].normalizedTime < endTime)
                        {
                            if (!HumanCache.BladeHitLeft.IsActive())
                            {
                                HumanCache.BladeHitLeft.Activate();
                                PlaySound(HumanSounds.BladeSwing);
                                ToggleBladeTrails(true);
                            }
                            if (!HumanCache.BladeHitRight.IsActive())
                                HumanCache.BladeHitRight.Activate();
                        }
                        else if (HumanCache.BladeHitLeft.IsActive())
                        {
                            HumanCache.BladeHitLeft.Deactivate();
                            HumanCache.BladeHitRight.Deactivate();
                            ToggleBladeTrails(false, 0.1f);
                        }
                        if (Cache.Animation[AttackAnimation].normalizedTime >= 1f)
                            Idle();
                    }
                    else if (Setup.Weapon == HumanWeapon.Gun || Setup.Weapon == HumanWeapon.Thunderspear)
                    {
                        if (Cache.Animation[AttackAnimation].normalizedTime >= 1f)
                            Idle();
                    }
                }
                else if (State == HumanState.EmoteAction || State == HumanState.SpecialAction || State == HumanState.Stun)
                {
                    if (_stateTimeLeft <= 0f)
                        Idle();
                }
                else if (State == HumanState.GroundDodge)
                {
                    if (Cache.Animation.IsPlaying("dodge"))
                    {
                        if (!(Grounded || (Cache.Animation["dodge"].normalizedTime <= 0.6f)))
                            Idle();
                        if (Cache.Animation["dodge"].normalizedTime >= 1f)
                            Idle();
                    }
                }
                else if (State == HumanState.Land)
                {
                    if (Cache.Animation.IsPlaying("dash_land") && (Cache.Animation["dash_land"].normalizedTime >= 1f))
                        Idle();
                }
                else if (State == HumanState.Refill)
                {
                    if (_stateTimeLeft <= 0f)
                    {
                        Idle();
                        FinishRefill();
                    }
                }
                else if (State == HumanState.Reload)
                {
                    if (Weapon is BladeWeapon)
                    {
                        if (Grounded && Cache.Animation[_reloadAnimation].normalizedTime > 0.5f)
                            FinishReload();
                        else if (!Grounded && Cache.Animation[_reloadAnimation].normalizedTime > 0.56f)
                            FinishReload();
                    }
                    else
                    {
                        if (Cache.Animation[_reloadAnimation].normalizedTime > 0.62f)
                            FinishReload();
                    }
                    if (_stateTimeLeft <= 0f)
                        Idle();
                }
                else if (State == HumanState.Slide)
                {
                    if (!Grounded)
                        Idle();
                }
                else if (State == HumanState.AirDodge)
                {
                    if (_dashTimeLeft > 0f)
                    {
                        _dashTimeLeft -= Time.deltaTime;
                        if (Cache.Rigidbody.velocity.magnitude > _originalDashSpeed)
                            Cache.Rigidbody.AddForce(-Cache.Rigidbody.velocity * Time.deltaTime * 1.7f, ForceMode.VelocityChange);
                    }
                    else
                        Idle();
                }
            }
        }

        protected void FixedUpdate()
        {
            if (IsMine())
            {
                FixedUpdateLookTitan();
                FixedUpdateUseables();
                if (State == HumanState.Grab || Dead)
                {
                    Cache.Rigidbody.velocity = Vector3.zero;
                    return;
                }
                if (MountState == HumanMountState.Horse)
                {
                    Cache.Rigidbody.velocity = Horse.Cache.Rigidbody.velocity;
                    return;
                }
                if (MountState == HumanMountState.MapObject)
                {
                    Cache.Rigidbody.velocity = Vector3.zero;
                    ToggleSparks(false);
                    if (State != HumanState.Idle)
                    {
                        Idle();
                    }
                    return;
                }
                if (_hookHuman != null)
                {
                    Vector3 vector2 = _hookHuman.Cache.Transform.position - Cache.Transform.position;
                    float magnitude = vector2.magnitude;
                    if (magnitude > 2f)
                        Cache.Rigidbody.AddForce((vector2.normalized * Mathf.Pow(magnitude, 0.15f) * 30f) - (Cache.Rigidbody.velocity * 0.95f), ForceMode.VelocityChange);
                }
                Vector3 currentVelocity = Cache.Rigidbody.velocity;
                float currentSpeed = Cache.Rigidbody.velocity.magnitude;
                GameProgressManager.RegisterSpeed(gameObject, currentSpeed);
                Cache.Transform.rotation = Quaternion.Lerp(Cache.Transform.rotation, _targetRotation, Time.deltaTime * 6f);
                CheckGround();
                bool pivotLeft = FixedUpdateLaunch(true);
                bool pivotRight = FixedUpdateLaunch(false);
                bool pivot = pivotLeft || pivotRight;
                if (Grounded)
                {
                    Vector3 newVelocity = Vector3.zero;
                    if (State == HumanState.Attack)
                    {
                        if (Cache.Animation.IsPlaying("attack1") || Cache.Animation.IsPlaying("attack2"))
                            Cache.Rigidbody.AddForce(Cache.Transform.forward * 200f);
                    }
                    if (JustGrounded)
                    {
                        if (State != HumanState.Attack && State != HumanState.SpecialAttack && State != HumanState.SpecialAction 
                            && State != HumanState.Stun && !HasDirection && !HasHook())
                        {
                            State = HumanState.Land;
                            CrossFade(HumanAnimations.Land, 0.01f);
                            if (!Cache.AudioSources[HumanSounds.Land].isPlaying)
                                PlaySound(HumanSounds.Land);
                        }
                        else
                        {
                            _attackButtonRelease = true;
                            Vector3 v = Cache.Rigidbody.velocity;
                            if (State != HumanState.Attack && State != HumanState.SpecialAttack && State != HumanState.SpecialAction && State != HumanState.Stun && 
                                State != HumanState.EmoteAction && (v.x * v.x + v.z * v.z > RunSpeed * RunSpeed * 1.5f) && State != HumanState.Refill)
                            {
                                State = HumanState.Slide;
                                CrossFade(HumanAnimations.Slide, 0.05f);
                                TargetAngle = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                                _targetRotation = GetTargetRotation();
                                HasDirection = true;
                                ToggleSparks(true);
                            }
                        }
                        newVelocity = Cache.Rigidbody.velocity;
                    }
                    if (State == HumanState.GroundDodge)
                    {
                        if (Cache.Animation[HumanAnimations.Dodge].normalizedTime >= 0.2f && Cache.Animation[HumanAnimations.Dodge].normalizedTime < 0.8f)
                            newVelocity = -Cache.Transform.forward * 2.4f * RunSpeed;
                        else if (Cache.Animation[HumanAnimations.Dodge].normalizedTime > 0.8f)
                            newVelocity = Cache.Rigidbody.velocity * 0.9f;
                    }
                    else if (State == HumanState.Idle)
                    {
                        newVelocity = Vector3.zero;
                        if (HasDirection)
                        {
                            newVelocity = GetTargetDirection() * TargetMagnitude * RunSpeed;
                            if (!Cache.Animation.IsPlaying(HumanAnimations.Run) && !Cache.Animation.IsPlaying(HumanAnimations.Jump) &&
                                !Cache.Animation.IsPlaying(HumanAnimations.RunBuffed) && (!Cache.Animation.IsPlaying(HumanAnimations.HorseMount) ||
                                Cache.Animation[HumanAnimations.HorseMount].normalizedTime >= 0.5f))
                            {
                                CrossFade(RunAnimation, 0.1f);
                                _stepPhase = 0;
                            }
                            _targetRotation = GetTargetRotation();
                        }
                        else if (!(Cache.Animation.IsPlaying(StandAnimation) || State == HumanState.Land || Cache.Animation.IsPlaying(HumanAnimations.Jump) || Cache.Animation.IsPlaying(HumanAnimations.HorseMount) || Cache.Animation.IsPlaying(HumanAnimations.Grabbed)))
                        {
                            CrossFade(StandAnimation, 0.1f);
                        }
                    }
                    else if (State == HumanState.Land)
                    {
                        newVelocity = Cache.Rigidbody.velocity * 0.96f;
                    }
                    else if (State == HumanState.Slide)
                    {
                        newVelocity = Cache.Rigidbody.velocity * 0.99f;
                        if (currentSpeed < RunSpeed * 1.2f)
                        {
                            Idle();
                            ToggleSparks(false);
                        }
                    }
                    Vector3 force = newVelocity - currentVelocity;
                    force.x = Mathf.Clamp(force.x, -MaxVelocityChange, MaxVelocityChange);
                    force.z = Mathf.Clamp(force.z, -MaxVelocityChange, MaxVelocityChange);
                    force.y = 0f;
                    if (Cache.Animation.IsPlaying(HumanAnimations.Jump) && Cache.Animation[HumanAnimations.Jump].normalizedTime > 0.18f)
                        force.y += 8f;
                    if (Cache.Animation.IsPlaying(HumanAnimations.HorseMount) && Cache.Animation[HumanAnimations.HorseMount].normalizedTime > 0.18f && Cache.Animation[HumanAnimations.HorseMount].normalizedTime < 1f)
                    {
                        force = -currentVelocity;
                        force.y = 6f;
                        float distance = Vector3.Distance(Horse.Cache.Transform.position, Cache.Transform.position);
                        force += (Horse.Cache.Transform.position - Cache.Transform.position).normalized * 0.6f * Gravity.magnitude * distance / 12f;
                    }
                    Cache.Rigidbody.AddForce(force, ForceMode.VelocityChange);
                    Cache.Rigidbody.rotation = Quaternion.Lerp(Cache.Transform.rotation, Quaternion.Euler(0f, TargetAngle, 0f), Time.deltaTime * 10f);
                }
                else
                {
                    ToggleSparks(false);
                    if (Horse != null && (Cache.Animation.IsPlaying(HumanAnimations.HorseMount) || Cache.Animation.IsPlaying("air_fall")) && Cache.Rigidbody.velocity.y < 0f && Vector3.Distance(Horse.Cache.Transform.position + Vector3.up * 1.65f, Cache.Transform.position) < 0.5f)
                    {
                        Cache.Transform.position = Horse.Cache.Transform.position + Vector3.up * 1.65f;
                        Cache.Transform.rotation = Horse.Cache.Transform.rotation;
                        MountState = HumanMountState.Horse;
                        SetInterpolation(false);
                        if (!Cache.Animation.IsPlaying("horse_idle"))
                            CrossFade("horse_idle", 0.1f);
                    }
                    if (Cache.Animation[HumanAnimations.Dash].normalizedTime >= 0.99f || (State == HumanState.Idle && !Cache.Animation.IsPlaying(HumanAnimations.Dash) && !Cache.Animation.IsPlaying("wallrun") && !Cache.Animation.IsPlaying("toRoof")
                        && !Cache.Animation.IsPlaying(HumanAnimations.HorseMount) && !Cache.Animation.IsPlaying(HumanAnimations.HorseDismount) && !Cache.Animation.IsPlaying("air_release")
                        && MountState == HumanMountState.None && (!Cache.Animation.IsPlaying("air_hook_l_just") || Cache.Animation["air_hook_l_just"].normalizedTime >= 1f) && (!Cache.Animation.IsPlaying("air_hook_r_just") || Cache.Animation["air_hook_r_just"].normalizedTime >= 1f)))
                    {
                        if (!IsHookedAny() && (Cache.Animation.IsPlaying("air_hook_l") || Cache.Animation.IsPlaying("air_hook_r") || Cache.Animation.IsPlaying("air_hook")) && Cache.Rigidbody.velocity.y > 20f)
                        {
                            CrossFade("air_release");
                        }
                        else
                        {
                            if ((Mathf.Abs(currentVelocity.x) + Mathf.Abs(currentVelocity.z)) <= 25f)
                            {
                                if (currentVelocity.y < 0f)
                                {
                                    if (!Cache.Animation.IsPlaying("air_fall"))
                                        CrossFade("air_fall", 0.2f);
                                }
                                else if (!Cache.Animation.IsPlaying("air_rise"))
                                    CrossFade("air_rise", 0.2f);
                            }
                            else if (!IsHookedAny())
                            {
                                float angle = -Mathf.DeltaAngle(-Mathf.Atan2(currentVelocity.z, currentVelocity.x) * Mathf.Rad2Deg, Cache.Transform.rotation.eulerAngles.y - 90f);
                                if (Mathf.Abs(angle) < 45f)
                                {
                                    if (!Cache.Animation.IsPlaying("air2"))
                                        CrossFade("air2", 0.2f);
                                }
                                else if ((angle < 135f) && (angle > 0f))
                                {
                                    if (!Cache.Animation.IsPlaying("air2_right"))
                                        CrossFade("air2_right", 0.2f);
                                }
                                else if ((angle > -135f) && (angle < 0f))
                                {
                                    if (!Cache.Animation.IsPlaying("air2_left"))
                                        CrossFade("air2_left", 0.2f);
                                }
                                else if (!Cache.Animation.IsPlaying("air2_backward"))
                                    CrossFade("air2_backward", 0.2f);
                            }
                            else if (Setup.Weapon == HumanWeapon.Gun)
                            {
                                if (IsHookedLeft())
                                {
                                    if (!Cache.Animation.IsPlaying("AHSS_hook_forward_l"))
                                        CrossFade("AHSS_hook_forward_l", 0.1f);
                                }
                                else if (IsHookedRight())
                                {
                                    if (!Cache.Animation.IsPlaying("AHSS_hook_forward_r"))
                                        CrossFade("AHSS_hook_forward_r", 0.1f);
                                }
                                else if (!Cache.Animation.IsPlaying("AHSS_hook_forward_both"))
                                    CrossFade("AHSS_hook_forward_both", 0.1f);
                            }
                            else if (!IsHookedRight())
                            {
                                if (!Cache.Animation.IsPlaying("air_hook_l"))
                                    CrossFade("air_hook_l", 0.1f);
                            }
                            else if (!IsHookedLeft())
                            {
                                if (!Cache.Animation.IsPlaying("air_hook_r"))
                                    CrossFade("air_hook_r", 0.1f);
                            }
                            else if (!Cache.Animation.IsPlaying("air_hook"))
                                CrossFade("air_hook", 0.1f);
                        }
                    }
                    if (!Cache.Animation.IsPlaying("air_rise"))
                    {
                        if (State == HumanState.Idle && Cache.Animation.IsPlaying("air_release") && Cache.Animation["air_release"].normalizedTime >= 1f)
                            CrossFade("air_rise", 0.2f);
                        else if (Cache.Animation.IsPlaying(HumanAnimations.HorseDismount) && Cache.Animation[HumanAnimations.HorseDismount].normalizedTime >= 1f)
                            CrossFade("air_rise", 0.2f);
                    }
                    if (Cache.Animation.IsPlaying("toRoof"))
                    {
                        if (Cache.Animation["toRoof"].normalizedTime < 0.22f)
                        {
                            Cache.Rigidbody.velocity = Vector3.zero;
                            Cache.Rigidbody.AddForce(new Vector3(0f, Gravity.magnitude * Cache.Rigidbody.mass, 0f));
                        }
                        else
                        {
                            if (!_wallJump)
                            {
                                _wallJump = true;
                                Cache.Rigidbody.AddForce(Vector3.up * 8f, ForceMode.Impulse);
                            }
                            Cache.Rigidbody.AddForce(Cache.Transform.forward * 0.05f, ForceMode.Impulse);
                        }
                        if (Cache.Animation["toRoof"].normalizedTime >= 1f)
                        {
                            PlayAnimation("air_rise");
                        }
                    }
                    else if (!(State != HumanState.Idle || !IsPressDirectionTowardsHero() || SettingsManager.InputSettings.Human.Jump.GetKey() || SettingsManager.InputSettings.Human.HookLeft.GetKey() || SettingsManager.InputSettings.Human.HookRight.GetKey() || SettingsManager.InputSettings.Human.HookBoth.GetKey() || !IsFrontGrounded() || Cache.Animation.IsPlaying("wallrun") || Cache.Animation.IsPlaying("dodge")))
                    {
                        CrossFade("wallrun", 0.1f);
                        _wallRunTime = 0f;
                    }
                    else if (Cache.Animation.IsPlaying("wallrun"))
                    {
                        Cache.Rigidbody.AddForce(Vector3.up * RunSpeed - Cache.Rigidbody.velocity, ForceMode.VelocityChange);
                        _wallRunTime += Time.deltaTime;
                        if (_wallRunTime > 1f || !HasDirection)
                        {
                            Cache.Rigidbody.AddForce(-Cache.Transform.forward * RunSpeed * 0.75f, ForceMode.Impulse);
                            DodgeWall();
                        }
                        else if (!IsUpFrontGrounded())
                        {
                            _wallJump = false;
                            CrossFade("toRoof", 0.1f);
                        }
                        else if (!IsFrontGrounded())
                            CrossFade("air_fall", 0.1f);
                    }
                    else if (!Cache.Animation.IsPlaying("dash") && !Cache.Animation.IsPlaying("jump") && !IsFiringThunderspear())
                    {
                        Vector3 targetDirection = GetTargetDirection() * TargetMagnitude * Setup.CustomSet.Acceleration.Value / 5f;
                        if (!HasDirection)
                        {
                            if (State == HumanState.Attack)
                                targetDirection = Vector3.zero;
                        }
                        else
                            _targetRotation = GetTargetRotation();
                        if (((!pivotLeft && !pivotRight) && (MountState == HumanMountState.None && SettingsManager.InputSettings.Human.Jump.GetKey())) && (CurrentGas > 0f))
                        {
                            if (HasDirection)
                            {
                                Cache.Rigidbody.AddForce(targetDirection, ForceMode.Acceleration);
                            }
                            else
                            {
                                Cache.Rigidbody.AddForce((Cache.Transform.forward * targetDirection.magnitude), ForceMode.Acceleration);
                            }
                            pivot = true;
                        }
                    }
                    if ((Cache.Animation.IsPlaying("air_fall") && (currentSpeed < 0.2f)) && this.IsFrontGrounded())
                    {
                        CrossFade("onWall", 0.3f);
                    }
                }
                if (pivotLeft && pivotRight)
                    FixedUpdatePivot((HookRight.GetHookPosition() + HookLeft.GetHookPosition()) * 0.5f);
                else if (pivotLeft)
                    FixedUpdatePivot(HookLeft.GetHookPosition());
                else if (pivotRight)
                    FixedUpdatePivot(HookRight.GetHookPosition());
                bool lowerGravity = false;
                if (IsHookedLeft() && HookLeft.GetHookPosition().y > Cache.Transform.position.y && _launchLeft)
                    lowerGravity = true;
                else if (IsHookedRight() && HookRight.GetHookPosition().y > Cache.Transform.position.y && _launchRight)
                    lowerGravity = true;
                Vector3 gravity;
                if (lowerGravity)
                    gravity = Gravity * 0.5f * Cache.Rigidbody.mass;
                else
                    gravity = Gravity * Cache.Rigidbody.mass;
                Cache.Rigidbody.AddForce(gravity);
                if (!_cancelGasDisable)
                {
                    if (pivot)
                    {
                        UseGas(GasUsage * Time.deltaTime);
                        if (!HumanCache.Smoke.enableEmission)
                            Cache.PhotonView.RPC("SetSmokeRPC", PhotonTargets.All, new object[] { true });
                    }
                    else
                    {
                        if (HumanCache.Smoke.enableEmission)
                            Cache.PhotonView.RPC("SetSmokeRPC", PhotonTargets.All, new object[] { false });
                    }
                }
                else
                    _cancelGasDisable = false;
                if (WindWeatherEffect.WindEnabled)
                {
                    if (!HumanCache.Wind.enableEmission)
                        HumanCache.Wind.enableEmission = true;
                    HumanCache.Wind.startSpeed = 100f;
                    HumanCache.WindTransform.LookAt(Cache.Transform.position + WindWeatherEffect.WindDirection);
                }
                else if (currentSpeed > 80f && SettingsManager.GraphicsSettings.WindEffectEnabled.Value)
                {
                    if (!HumanCache.Wind.enableEmission)
                        HumanCache.Wind.enableEmission = true;
                    HumanCache.Wind.startSpeed = currentSpeed;
                    HumanCache.WindTransform.LookAt(Cache.Transform.position - currentVelocity);
                }
                else if (HumanCache.Wind.enableEmission)
                    HumanCache.Wind.enableEmission = false;
                FixedUpdateSetHookedDirection();
                FixedUpdateBodyLean();
                ReelInAxis = 0f;
            }
        }

        protected override void LateUpdate()
        {
            if (IsMine() && MountState == HumanMountState.None && State != HumanState.Grab)
            {
                LateUpdateTilt();
                LateUpdateGun();
            }
            base.LateUpdate();
        }

        private void UpdateBladeTrails()
        {
            if (Setup.LeftTrail1 != null && Setup.LeftTrail1.gameObject.activeSelf)
            {
                Setup.LeftTrail1.update();
                Setup.RightTrail1.update();
            }
            if (Setup.LeftTrail2 != null && Setup.LeftTrail2.gameObject.activeSelf)
            {
                Setup.LeftTrail2.update();
                Setup.RightTrail2.update();
            }
            if (Setup.LeftTrail1 != null && Setup.LeftTrail1.gameObject.activeSelf)
            {
                Setup.LeftTrail1.lateUpdate();
                Setup.RightTrail1.lateUpdate();
            }
            if (Setup.LeftTrail2 != null && Setup.LeftTrail2.gameObject.activeSelf)
            {
                Setup.LeftTrail2.lateUpdate();
                Setup.RightTrail2.lateUpdate();
            }
        }

        private bool FixedUpdateLaunch(bool left)
        {
            bool launch;
            HookUseable hook;
            bool pivot = false;
            float launchTime;
            if (left)
            {
                launch = _launchLeft;
                hook = HookLeft;
                _launchLeftTime += Time.deltaTime;
                launchTime = _launchLeftTime;
            }
            else
            {
                launch = _launchRight;
                hook = HookRight;
                _launchRightTime += Time.deltaTime;
                launchTime = _launchRightTime;
            }
            if (launch)
            {
                if (hook.IsHooked())
                {
                    Vector3 v = (hook.GetHookPosition() - Cache.Transform.position).normalized * 10f;
                    if (!(_launchLeft && _launchRight))
                        v *= 2f;
                    if ((Vector3.Angle(Cache.Rigidbody.velocity, v) > 90f) && SettingsManager.InputSettings.Human.Jump.GetKey())
                    {
                        pivot = true;
                    }
                    if (!pivot)
                    {
                        Cache.Rigidbody.AddForce(v);
                        if (Vector3.Angle(Cache.Rigidbody.velocity, v) > 90f)
                            Cache.Rigidbody.AddForce(-Cache.Rigidbody.velocity * 2f, ForceMode.Acceleration);
                    }
                }
                if (hook.IsActive && CurrentGas > 0f)
                    UseGas(GasUsage * Time.deltaTime);
                else if (launchTime > 0.3f)
                {
                    if (left)
                        _launchLeft = false;
                    else
                        _launchRight = false;
                    hook.DisableActiveHook();
                    UnhookHuman(left);
                    pivot = false;
                }
            }
            return pivot;
        }

        private void FixedUpdatePivot(Vector3 position)
        {
            float newSpeed = Cache.Rigidbody.velocity.magnitude + 0.1f;
            Cache.Rigidbody.AddForce(-Cache.Rigidbody.velocity, ForceMode.VelocityChange);
            Vector3 v = position - Cache.Transform.position;
            float reel = GetReelAxis();
            reel = Mathf.Clamp(reel, -0.8f, 0.8f) + 1f;
            v = Vector3.RotateTowards(v, Cache.Rigidbody.velocity, 1.53938f * reel, 1.53938f * reel).normalized;
            Cache.Rigidbody.velocity = (v * newSpeed);
        }

        private void FixedUpdateSetHookedDirection()
        {
            _almostSingleHook = false;
            float oldTargetAngle = TargetAngle;
            if (IsHookedLeft() && IsHookedRight())
            {
                Vector3 hookDiff = HookLeft.GetHookPosition() - HookRight.GetHookPosition();
                Vector3 direction = (HookLeft.GetHookPosition() + HookRight.GetHookPosition()) * 0.5f - Cache.Transform.position;
                if (hookDiff.sqrMagnitude < 4f)
                {
                    TargetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    if (Setup.Weapon == HumanWeapon.Gun && State != HumanState.Attack)
                    {
                        float current = -Mathf.Atan2(Cache.Rigidbody.velocity.z, Cache.Rigidbody.velocity.x) * Mathf.Rad2Deg;
                        float target = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                        TargetAngle -= Mathf.DeltaAngle(current, target);
                    }
                    _almostSingleHook = true;
                }
                else
                {
                    Vector3 left = Cache.Transform.position - HookLeft.GetHookPosition();
                    Vector3 right = Cache.Transform.position - HookRight.GetHookPosition();
                    if (Vector3.Angle(direction, left) < 30f && Vector3.Angle(direction, right) < 30f)
                    {
                        _almostSingleHook = true;
                        TargetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    }
                    else
                    {
                        _almostSingleHook = false;
                        Vector3 forward = Cache.Transform.forward;
                        Vector3.OrthoNormalize(ref hookDiff, ref forward);
                        TargetAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                        float angle = Mathf.Atan2(left.x, left.z) * Mathf.Rad2Deg;
                        if (Mathf.DeltaAngle(angle, TargetAngle) > 0f)
                            TargetAngle += 180f;
                    }
                }
            }
            else
            {
                _almostSingleHook = true;
                Vector3 v;
                if (IsHookedLeft())
                    v = HookLeft.GetHookPosition() - Cache.Transform.position;
                else if (IsHookedRight())
                    v = HookRight.GetHookPosition() - Cache.Transform.position;
                else
                    return;
                TargetAngle = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
                if (State != HumanState.Attack)
                {
                    float angle1 = -Mathf.Atan2(Cache.Rigidbody.velocity.z, Cache.Rigidbody.velocity.x) * Mathf.Rad2Deg;
                    float angle2 = -Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
                    float delta = -Mathf.DeltaAngle(angle1, angle2);
                    if (Setup.Weapon == HumanWeapon.Gun)
                        TargetAngle += delta;
                    else
                    {
                        float multiplier = 0.1f;
                        if ((IsHookedLeft() && delta < 0f) || (IsHookedRight() && delta > 0f))
                            multiplier = -0.1f;
                        TargetAngle += delta * multiplier;
                    }
                }
            }
            if (IsFiringThunderspear())
                TargetAngle = oldTargetAngle;
        }

        private void FixedUpdateBodyLean()
        {
            float z = 0f;
            _needLean = false;
            if (Setup.Weapon != HumanWeapon.Gun && State == HumanState.Attack && !IsFiringThunderspear())
            {
                Vector3 v = Cache.Rigidbody.velocity;
                float diag = Mathf.Sqrt((v.x * v.x) + (v.z * v.z));
                float angle = Mathf.Atan2(v.y, diag) * Mathf.Rad2Deg;
                _targetRotation = Quaternion.Euler(-angle * (1f - (Vector3.Angle(v, Cache.Transform.forward) / 90f)), TargetAngle, 0f);
                if (IsHookedAny())
                    Cache.Transform.rotation = _targetRotation;
            }
            else
            {
                if (IsHookedLeft() && IsHookedRight())
                {
                    if (_almostSingleHook)
                    {
                        _needLean = true;
                        z = GetLeanAngle(HookRight.GetHookPosition(), true);
                    }
                }
                else if (IsHookedLeft())
                {
                    _needLean = true;
                    z = GetLeanAngle(HookLeft.GetHookPosition(), true);
                }
                else if (IsHookedRight())
                {
                    _needLean = true;
                    z = GetLeanAngle(HookRight.GetHookPosition(), false);

                }
                if (_needLean)
                {
                    float a = 0f;
                    if (Setup.Weapon != HumanWeapon.Gun && State != HumanState.Attack)
                    {
                        a = Cache.Rigidbody.velocity.magnitude * 0.1f;
                        a = Mathf.Min(a, 20f);
                    }
                    _targetRotation = Quaternion.Euler(-a, TargetAngle, z);
                }
                else if (State != HumanState.Attack)
                    _targetRotation = Quaternion.Euler(0f, TargetAngle, 0f);
            }
        }

        private void FixedUpdateUseables()
        {
            if (FinishSetup)
            {
                Weapon.OnFixedUpdate();
                HookLeft.OnFixedUpdate();
                HookRight.OnFixedUpdate();
                if (Special != null)
                    Special.OnFixedUpdate();
            }
        }

        public void FixedUpdateLookTitan()
        {
            Ray ray = SceneLoader.CurrentCamera.Camera.ScreenPointToRay(Input.mousePosition);
            LayerMask mask = PhysicsLayer.GetMask(PhysicsLayer.EntityDetection);
            RaycastHit[] hitArr = Physics.RaycastAll(ray, 200f, mask.value);
            if (hitArr.Length == 0)
                return;
            List<RaycastHit> hitList = new List<RaycastHit>(hitArr);
            hitList.Sort((x, y) => x.distance.CompareTo(y.distance));
            int maxCount = Math.Min(hitList.Count, 3);
            for (int i = 0; i < maxCount; i++)
            {
                var entity = hitList[i].collider.GetComponent<TitanEntityDetection>();
                entity.Owner.TitanColliderToggler.RegisterLook();
            }
        }

        private void LateUpdateTilt()
        {
            if (IsMainCharacter() && SettingsManager.GeneralSettings.CameraTilt.Value)
            {
                Quaternion rotation;
                Vector3 left = Vector3.zero;
                Vector3 right = Vector3.zero;
                if (_launchLeft && IsHookedLeft())
                    left = HookLeft.GetHookPosition();
                if (_launchRight && IsHookedRight())
                    right = HookRight.GetHookPosition();
                Vector3 target = Vector3.zero;
                if (left.magnitude != 0f && right.magnitude == 0f)
                    target = left;
                else if (right.magnitude != 0f && left.magnitude == 0f)
                    target = right;
                else if (left.magnitude != 0f && right.magnitude != 0f)
                    target = 0.5f * (left + right);
                Transform camera = SceneLoader.CurrentCamera.Cache.Transform;
                Vector3 projectUp = Vector3.Project(target - Cache.Transform.position, camera.up);
                Vector3 projectRight = Vector3.Project(target - Cache.Transform.position, camera.right);
                if (target.magnitude > 0f)
                {
                    Vector3 projectDirection = projectUp + projectRight;
                    float angle = Vector3.Angle(target - Cache.Transform.position, Cache.Rigidbody.velocity) * 0.005f;
                    Vector3 finalRight = camera.right + projectRight.normalized;
                    float finalAngle = Vector3.Angle(projectUp, projectDirection) * angle;
                    rotation = Quaternion.Euler(camera.rotation.eulerAngles.x, camera.rotation.eulerAngles.y, (finalRight.magnitude >= 1f) ? -finalAngle : finalAngle);
                }
                else
                    rotation = Quaternion.Euler(camera.rotation.eulerAngles.x, camera.rotation.eulerAngles.y, 0f);
                camera.rotation = Quaternion.Lerp(camera.rotation, rotation, Time.deltaTime * 2f);
            }
        }

        private void LateUpdateGun()
        {
            if (Setup.Weapon == HumanWeapon.Gun)
            {
                if (_leftArmAim || _rightArmAim)
                {
                    Vector3 direction = _gunTarget - Cache.Transform.position;
                    float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                    float delta = -Mathf.DeltaAngle(angle, Cache.Transform.rotation.eulerAngles.y - 90f);
                    GunHeadMovement();
                    if (!IsHookedLeft() && _leftArmAim && delta < 40f && delta > -90f)
                        LeftArmAim(_gunTarget);
                    if (!IsHookedRight() && _rightArmAim && delta > -40f && delta < 90f)
                        RightArmAim(_gunTarget);
                }
                else if (!Grounded)
                {
                    HumanCache.HandL.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    HumanCache.HandR.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                }
                if (IsHookedLeft())
                    LeftArmAim(HookLeft.GetHookPosition());
                if (IsHookedRight())
                    RightArmAim(HookRight.GetHookPosition());
            }
        }

        private void GunHeadMovement()
        {
            return;
            Vector3 position = Cache.Transform.position;
            float x = Mathf.Sqrt(Mathf.Pow(_gunTarget.x - position.x, 2f) + Mathf.Pow(_gunTarget.z - position.z, 2f));
            var originalRotation = Cache.Transform.rotation;
            var targetRotation = originalRotation;
            Vector3 euler = originalRotation.eulerAngles;
            Vector3 direction = _gunTarget - position;
            float angle = -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
            float deltaY = -Mathf.DeltaAngle(angle, euler.y - 90f);
            deltaY = Mathf.Clamp(deltaY, -40f, 40f);
            float y = HumanCache.Neck.position.y - _gunTarget.y;
            float deltaX = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            deltaX = Mathf.Clamp(deltaX, -40f, 30f);
            targetRotation = Quaternion.Euler(euler.x + deltaX, euler.y + deltaY, euler.z);
            Cache.Transform.rotation = Quaternion.Lerp(originalRotation, targetRotation, Time.deltaTime * 60f);
        }

        private void LeftArmAim(Vector3 target)
        {
            float y = target.x - HumanCache.UpperarmL.position.x;
            float num2 = target.y - HumanCache.UpperarmL.position.y;
            float x = target.z - HumanCache.UpperarmL.position.z;
            float num4 = Mathf.Sqrt((y * y) + (x * x));
            HumanCache.HandL.localRotation = Quaternion.Euler(90f, 0f, 0f);
            HumanCache.ForearmL.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            HumanCache.UpperarmL.rotation = Quaternion.Euler(0f, 90f + (Mathf.Atan2(y, x) * 57.29578f), -Mathf.Atan2(num2, num4) * 57.29578f);
        }

        private void RightArmAim(Vector3 target)
        {
            float y = target.x - HumanCache.UpperarmR.position.x;
            float num2 = target.y - HumanCache.UpperarmR.position.y;
            float x = target.z - HumanCache.UpperarmR.position.z;
            float num4 = Mathf.Sqrt((y * y) + (x * x));
            HumanCache.HandR.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            HumanCache.ForearmR.localRotation = Quaternion.Euler(90f, 0f, 0f);
            HumanCache.UpperarmR.rotation = Quaternion.Euler(180f, 90f + (Mathf.Atan2(y, x) * 57.29578f), Mathf.Atan2(num2, num4) * 57.29578f);
        }

        protected override void SetColliders()
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                if (c.name == "checkBox")
                    c.gameObject.layer = PhysicsLayer.Hitbox;
                else
                    c.gameObject.layer = PhysicsLayer.NoCollision;
            }
            gameObject.layer = PhysicsLayer.Human;
        }

        [RPC]
        public void SetupRPC(string customSetJson, int humanWeapon, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            HumanCustomSet set = new HumanCustomSet();
            set.DeserializeFromJsonString(customSetJson);
            Setup.Load(set, (HumanWeapon)humanWeapon, false);
            HookLeft = new HookUseable(this, true, humanWeapon == (int)HumanWeapon.Gun);
            HookRight = new HookUseable(this, false, humanWeapon == (int)HumanWeapon.Gun);
            if (MaxGas == -1f)
                MaxGas = set.Gas.Value;
            if (CurrentGas == -1f)
                CurrentGas = MaxGas;
            SetAcceleration(set.Acceleration.Value);
            SetRunSpeed(set.Speed.Value);
            StandAnimation = HumanAnimations.StandFemale;
            RunAnimation = HumanAnimations.Run;
            if (humanWeapon == (int)HumanWeapon.Gun)
                StandAnimation = HumanAnimations.StandGun;
            else if (Setup.CustomSet.Sex.Value == (int)HumanSex.Male)
                StandAnimation = HumanAnimations.StandMale;
            if (IsMine())
            {
                SetupWeapon(set, humanWeapon);
                SetupItems();
                SetupSpecial();
            }
            FinishSetup = true;
        }

        public void SetAcceleration(int acceleration)
        {
            AccelerationStat = acceleration;
            Cache.Rigidbody.mass = 0.5f - (acceleration - 100) * 0.001f;
        }

        public void SetRunSpeed(int speed)
        {
            RunSpeedStat = speed;
            RunSpeed = speed / 10f;
        }

        protected void SetupWeapon(HumanCustomSet set, int humanWeapon)
        {
            if (humanWeapon == (int)HumanWeapon.Blade)
            {
                var bladeInfo = CharacterData.HumanWeaponInfo["Blade"];
                Weapon = new BladeWeapon(this, set.Blade.Value * bladeInfo["DurabilityMultiplier"].AsFloat, bladeInfo["Blades"].AsInt);
            }
            else if (humanWeapon == (int)HumanWeapon.Gun)
            {
                var gunInfo = CharacterData.HumanWeaponInfo["Gun"];
                Weapon = new GunWeapon(this, gunInfo["AmmoTotal"].AsInt, gunInfo["AmmoRound"].AsInt, gunInfo["CD"].AsFloat);
            }
            else if (humanWeapon == (int)HumanWeapon.Thunderspear)
            {
                if (SettingsManager.InGameCurrent.Misc.ThunderspearPVP.Value)
                {
                    int radiusStat = SettingsManager.AbilitySettings.BombRadius.Value;
                    int cdStat = SettingsManager.AbilitySettings.BombCooldown.Value;
                    int speedStat = SettingsManager.AbilitySettings.BombSpeed.Value;
                    int rangeStat = SettingsManager.AbilitySettings.BombRange.Value;
                    if (radiusStat + cdStat + speedStat + rangeStat > 16)
                    {
                        radiusStat = speedStat = 6;
                        rangeStat = 3;
                        cdStat = 1;
                    }
                    float travelTime = ((rangeStat * 60f) + 200f) / ((speedStat * 60f) + 200f);
                    float radius = (radiusStat * 4f) + 20f;
                    float cd = ((cdStat + 4) * -0.4f) + 5f;
                    float speed = (speedStat * 60f) + 200f;
                    Weapon = new ThunderspearWeapon(this, -1, -1, cd, radius, speed, travelTime, 0f);
                    if (CustomLogicManager.Evaluator.CurrentTime > 10f)
                        Weapon.SetCooldownLeft(5f);
                    else
                        Weapon.SetCooldownLeft(10f);
                }
                else
                {
                    var tsInfo = CharacterData.HumanWeaponInfo["Thunderspear"];
                    float travelTime = tsInfo["Range"].AsFloat / tsInfo["Speed"].AsFloat;
                    Weapon = new ThunderspearWeapon(this, tsInfo["AmmoTotal"].AsInt, tsInfo["AmmoRound"].AsInt, tsInfo["CD"].AsFloat, tsInfo["Radius"].AsFloat,
                        tsInfo["Speed"].AsFloat, travelTime, tsInfo["Delay"].AsFloat);
                }
            }
        }

        protected void SetupItems()
        {
            Items.Add(new FlareItem(this, "Green", new Color(0f, 1f, 0f, 0.7f), 10f));
            Items.Add(new FlareItem(this, "Red", new Color(1f, 0f, 0f, 0.7f), 10f));
            Items.Add(new FlareItem(this, "Black", new Color(0f, 0f, 0f, 0.7f), 10f));
            Items.Add(new FlareItem(this, "Purple", new Color(153f / 255, 0f, 204f / 255, 0.7f), 10f));
            Items.Add(new FlareItem(this, "Blue", new Color(0f, 102f / 255, 204f / 255, 0.7f), 10f));
            Items.Add(new FlareItem(this, "Yellow", new Color(1f, 1f, 0f, 0.7f), 10f));
        }

        protected void SetupSpecial()
        {
            var special = SettingsManager.InGameCharacterSettings.Special.Value;
            var loadout = SettingsManager.InGameCharacterSettings.Loadout.Value;
            Special = HumanSpecials.GetSpecialUseable(this, special);
            ((InGameMenu)UIManager.CurrentMenu).SetSpecialIcon(HumanSpecials.GetSpecialIcon(loadout, special));
        }

        protected void LoadSkin()
        {
            if (IsMine())
            {
                if (SettingsManager.CustomSkinSettings.Human.SkinsEnabled.Value)
                {
                    HumanCustomSkinSet set = (HumanCustomSkinSet)SettingsManager.CustomSkinSettings.Human.GetSelectedSet();
                    string url = string.Join(",", new string[] { set.Horse.Value, set.Hair.Value, set.Eye.Value, set.Glass.Value, set.Face.Value,
                set.Skin.Value, set.Costume.Value, set.Logo.Value, set.GearL.Value, set.GearR.Value, set.Gas.Value, set.Hoodie.Value,
                    set.WeaponTrail.Value, set.ThunderspearL.Value, set.ThunderspearR.Value, set.HookL.Value, set.HookLTiling.Value.ToString(),
                set.HookR.Value, set.HookRTiling.Value.ToString()});
                    int viewID = -1;
                    if (Horse != null)
                    {
                        viewID = Horse.gameObject.GetPhotonView().viewID;
                    }
                    Cache.PhotonView.RPC("LoadSkinRPC", PhotonTargets.AllBuffered, new object[] { viewID, url });
                }
            }
        }

        [RPC]
        public void LoadSkinRPC(int horse, string url, PhotonMessageInfo info)
        {
            if (info.sender != photonView.owner)
                return;
            HumanCustomSkinSettings settings = SettingsManager.CustomSkinSettings.Human;
            if (settings.SkinsEnabled.Value && (!settings.SkinsLocal.Value || photonView.isMine))
            {
                StartCoroutine(_customSkinLoader.LoadSkinsFromRPC(new object[] { horse, url }));
            }
        }

        [RPC]
        public void SetHookStateRPC(bool left, int hookId, int state, PhotonMessageInfo info)
        {
            if (left)
                HookLeft.Hooks[hookId].OnSetHookState(state, info);
            else
                HookRight.Hooks[hookId].OnSetHookState(state, info);
        }

        [RPC]
        public void SetHookingRPC(bool left, int hookId, Vector3 baseVelocity, Vector3 relativeVelocity, PhotonMessageInfo info)
        {
            if (left)
                HookLeft.Hooks[hookId].OnSetHooking(baseVelocity, relativeVelocity, info);
            else
                HookRight.Hooks[hookId].OnSetHooking(baseVelocity, relativeVelocity, info);
        }

        [RPC]
        public void SetHookedRPC(bool left, int hookId, Vector3 position, int viewId, int objectId, PhotonMessageInfo info)
        {
            if (left)
                HookLeft.Hooks[hookId].OnSetHooked(position, viewId, objectId, info);
            else
                HookRight.Hooks[hookId].OnSetHooked(position, viewId, objectId, info);
        }

        [RPC]
        public void SetSmokeRPC(bool active, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            HumanCache.Smoke.enableEmission = active;
        }

        protected void ToggleSparks(bool toggle)
        {
            ToggleSound(HumanSounds.Slide, toggle);
            if (toggle != HumanCache.Sparks.enableEmission)
                Cache.PhotonView.RPC("ToggleSparksRPC", PhotonTargets.All, new object[] { toggle });
        }

        [RPC]
        protected void ToggleSparksRPC(bool toggle, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            HumanCache.Sparks.enableEmission = toggle;
        }

        public void SetThunderspears(bool hasLeft, bool hasRight)
        {
            photonView.RPC("SetThunderspearsRPC", PhotonTargets.All, new object[] { hasLeft, hasRight });
        }

        [RPC]
        public void SetThunderspearsRPC(bool hasLeft, bool hasRight, PhotonMessageInfo info)
        {
            if (info.sender != photonView.owner)
                return;
            if (Setup.ThunderspearLModel != null)
                Setup.ThunderspearLModel.SetActive(hasLeft);
            if (Setup.ThunderspearRModel != null)
                Setup.ThunderspearRModel.SetActive(hasRight);
        }

        public void OnHooked(bool left, Vector3 position)
        {
            if (left)
            {
                _launchLeft = true;
                _launchLeftTime = 0f;
            }
            else
            {
                _launchRight = true;
                _launchRightTime = 0f;
            }
            if (State == HumanState.Grab || State == HumanState.Reload || MountState == HumanMountState.MapObject
                || State == HumanState.Stun)
                return;
            if (MountState == HumanMountState.Horse)
                Unmount(true);
            if (State != HumanState.Attack)
                Idle();
            Vector3 v = (position - Cache.Transform.position).normalized * 20f;
            if (IsHookedLeft() && IsHookedRight())
                v *= 0.8f;
            FalseAttack();
            Idle();
            if (Setup.Weapon == HumanWeapon.Gun)
                CrossFade("AHSS_hook_forward_both", 0.1f);
            else if (left && !IsHookedRight())
                CrossFade("air_hook_l_just", 0.1f);
            else if (!left && !IsHookedLeft())
                CrossFade("air_hook_r_just", 0.1f);
            else
            {
                CrossFade(HumanAnimations.Dash, 0.1f);
            }
            Vector3 force = v;
            if (v.y < 30f)
                force += Vector3.up * (30f - v.y);
            if (position.y >= Cache.Transform.position.y)
                force += Vector3.up * (position.y - Cache.Transform.position.y) * 10f;
            Cache.Rigidbody.AddForce(force);
            TargetAngle = Mathf.Atan2(force.x, force.z) * Mathf.Rad2Deg;
            _targetRotation = GetTargetRotation();
            Cache.Transform.rotation = _targetRotation;
            Cache.Rigidbody.rotation = _targetRotation;
            ToggleSparks(false);
            _cancelGasDisable = true;
        }

        public void OnHookedHuman(bool left, Vector3 position, Human human)
        {
            if (State == HumanState.Grab || MountState == HumanMountState.MapObject || State == HumanState.Stun)
                return;
            if (!human.Dead && human != this)
            {
                _hookHuman = human;
                _hookHumanLeft = left;
                human.Cache.PhotonView.RPC("OnHookedByHuman", human.Cache.PhotonView.owner, new object[] { Cache.PhotonView.viewID });
                Vector3 launchForce = position - Cache.Transform.position;
                float num = Mathf.Pow(launchForce.magnitude, 0.1f);
                if (Grounded)
                    Cache.Rigidbody.AddForce(Vector3.up * Mathf.Min(launchForce.magnitude * 0.2f, (10f)), ForceMode.Impulse);
                Cache.Rigidbody.AddForce(launchForce * num * 0.1f, ForceMode.Impulse);
            }
        }

        public void UnhookHuman(bool left)
        {
            if (left == _hookHumanLeft)
                _hookHuman = null;
        }

        [RPC]
        public void OnHookedByHuman(int viewId, PhotonMessageInfo info)
        {
            var human = Util.FindCharacterByViewId(viewId);
            if (IsMine() && human != null && !Dead && human.Cache.PhotonView.owner == info.sender &&
                State != HumanState.Grab && MountState == HumanMountState.None && human != this)
            {
                Vector3 direction = human.Cache.Transform.position - Cache.Transform.position;
                Cache.Rigidbody.AddForce(-Cache.Rigidbody.velocity * 0.9f, ForceMode.VelocityChange);
                float num = Mathf.Pow(direction.magnitude, 0.1f);
                if (Grounded)
                    Cache.Rigidbody.AddForce(Vector3.up * Mathf.Min(direction.magnitude * 0.2f, 10f), ForceMode.Impulse);
                Cache.Rigidbody.AddForce(direction * num * 0.1f, ForceMode.Impulse);
                CrossFade("dash", 0.05f, 0.1f / Cache.Animation["dash"].length);
                State = HumanState.Stun;
                _stateTimeLeft = StunTime;
                FalseAttack();
                float facingDirection = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                Quaternion quaternion = Quaternion.Euler(0f, facingDirection, 0f);
                Cache.Rigidbody.rotation = quaternion;
                Cache.Transform.rotation = quaternion;
                _targetRotation = quaternion;
                TargetAngle = facingDirection;
            }
        }

        private void SetInterpolation(bool interpolate)
        {
            if (IsMine())
            {
                _interpolate = interpolate;
                Cache.PhotonView.RPC("SetInterpolationRPC", PhotonTargets.All, new object[] { interpolate });
            }
        }

        [RPC]
        public void SetInterpolationRPC(bool interpolate, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            if (IsMine())
            {
                if (interpolate && SettingsManager.GraphicsSettings.InterpolationEnabled.Value)
                    Cache.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                else
                    Cache.Rigidbody.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                if (interpolate)
                    Cache.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                else
                    Cache.Rigidbody.interpolation = RigidbodyInterpolation.None;
            }
        }

        private float GetReelAxis()
        {
            if (ReelInAxis != 0f)
                return ReelInAxis;
            return ReelOutAxis;
        }

        private float GetLeanAngle(Vector3 hookPosition, bool left)
        {
            if (Setup.Weapon != HumanWeapon.Gun && State == HumanState.Attack)
                return 0f;
            float height = hookPosition.y - Cache.Transform.position.y;
            float dist = Vector3.Distance(hookPosition, Cache.Transform.position);
            float angle = Mathf.Acos(height / dist) * Mathf.Rad2Deg * 0.1f * (1f + Mathf.Pow(Cache.Rigidbody.velocity.magnitude, 0.2f));
            Vector3 v = hookPosition - Cache.Transform.position;
            float current = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(Cache.Rigidbody.velocity.x, Cache.Rigidbody.velocity.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(current, target);
            angle += Mathf.Abs(delta * 0.5f);
            if (State != HumanState.Attack)
                angle = Mathf.Min(angle, 80f);
            _leanLeft = delta > 0f;
            if (Setup.Weapon == HumanWeapon.Gun)
                return angle * (delta >= 0f ? 1f : -1f);
            float multiplier = 0.5f;
            if ((left && delta < 0f) || (!left && delta > 0f))
                multiplier = 0.1f;
            return angle * (delta >= 0f ? multiplier : -multiplier);
        }

        public bool CanBladeAttack()
        {
            return Weapon is BladeWeapon && ((BladeWeapon)Weapon).CurrentDurability > 0f && State == HumanState.Idle;
        }

        public void StartSpecialAttack(string animation)
        {
            if (State == HumanState.Attack || State == HumanState.SpecialAttack)
                FalseAttack();
            PlayAnimation(animation);
            State = HumanState.SpecialAttack;
            ToggleSparks(false);
        }

        public void ActivateBlades()
        {
            if (!HumanCache.BladeHitLeft.IsActive())
            {
                HumanCache.BladeHitLeft.Activate();
                ToggleBladeTrails(true);
            }
            if (!HumanCache.BladeHitRight.IsActive())
                HumanCache.BladeHitRight.Activate();
        }

        public void StartBladeSwing()
        {
            if (_needLean)
            {
                if (SettingsManager.InputSettings.General.Left.GetKey())
                    AttackAnimation = (UnityEngine.Random.Range(0, 100) >= 50) ? "attack1_hook_l1" : "attack1_hook_l2";
                else if (SettingsManager.InputSettings.General.Right.GetKey())
                    AttackAnimation = (UnityEngine.Random.Range(0, 100) >= 50) ? "attack1_hook_r1" : "attack1_hook_r2";
                else if (_leanLeft)
                    AttackAnimation = (UnityEngine.Random.Range(0, 100) >= 50) ? "attack1_hook_l1" : "attack1_hook_l2";
                else
                    AttackAnimation = (UnityEngine.Random.Range(0, 100) >= 50) ? "attack1_hook_r1" : "attack1_hook_r2";
            }
            else if (SettingsManager.InputSettings.General.Left.GetKey())
                AttackAnimation = "attack2";
            else if (SettingsManager.InputSettings.General.Right.GetKey())
                AttackAnimation = "attack1";
            else if (HookLeft.IsHooked() && HookLeft.GetHookParent() != null)
            {
                BaseCharacter character = HookLeft.GetHookCharacter();
                if (character != null && character is BaseTitan)
                    AttackAnimation = GetBladeAnimationTarget(((BaseTitan)character).BaseTitanCache.Neck);
                else
                    AttackAnimation = GetBladeAnimationMouse();
            }
            else if (HookRight.IsHooked() && HookRight.GetHookParent() != null)
            {
                BaseCharacter character = HookRight.GetHookCharacter();
                if (character != null && character is BaseTitan)
                    AttackAnimation = GetBladeAnimationTarget(((BaseTitan)character).BaseTitanCache.Neck);
                else
                    AttackAnimation = GetBladeAnimationMouse();
            }
            else
            {
                BaseTitan titan = FindNearestTitan();
                if (titan != null)
                    AttackAnimation = GetBladeAnimationTarget(titan.BaseTitanCache.Neck);
                else
                    AttackAnimation = GetBladeAnimationMouse();
            }
            if (Grounded)
            {
                Cache.Rigidbody.AddForce(Cache.Transform.forward * 200f);
            }
            PlayAnimationReset(AttackAnimation);
            _attackButtonRelease = false;
            State = HumanState.Attack;
            if (Grounded)
            {
                _attackRelease = true;
                _attackButtonRelease = true;
            }
            else
                _attackRelease = false;
            ToggleSparks(false);
        }

        private string GetBladeAnimationMouse()
        {
            if (Input.mousePosition.x < (Screen.width * 0.5))
                return "attack2";
            else
                return "attack1";
        }

        private string GetBladeAnimationTarget(Transform target)
        {
            Vector3 v = target.position - Cache.Transform.position;
            float current = -Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
            float delta = -Mathf.DeltaAngle(current, Cache.Transform.rotation.eulerAngles.y - 90f);
            if (((Mathf.Abs(delta) < 90f) && (v.magnitude < 6f)) && ((target.position.y <= (Cache.Transform.position.y + 2f)) && (target.position.y >= (Cache.Transform.position.y - 5f))))
                return "attack4";
            else if (delta > 0f)
                return "attack1";
            else
                return "attack2";
        }

        private BaseTitan FindNearestTitan()
        {
            float nearestDistance = float.PositiveInfinity;
            BaseTitan nearestTitan = null;
            foreach (BaseTitan titan in _inGameManager.Titans)
            {
                float distance = Vector3.Distance(Cache.Transform.position, titan.Cache.Transform.position);
                if (distance < nearestDistance)
                {
                    nearestTitan = titan;
                    nearestDistance = distance;
                }
            }
            foreach (BaseTitan titan in _inGameManager.Shifters)
            {
                float distance = Vector3.Distance(Cache.Transform.position, titan.Cache.Transform.position);
                if (distance < nearestDistance)
                {
                    nearestTitan = titan;
                    nearestDistance = distance;
                }
            }
            return nearestTitan;
        }


        private void FalseAttack()
        {
            if (Setup.Weapon == HumanWeapon.Gun || Setup.Weapon == HumanWeapon.Thunderspear)
            {
                if (!_attackRelease)
                    _attackRelease = true;
            }
            else
            {
                ToggleBladeTrails(false, 0.2f);
                HumanCache.BladeHitLeft.Deactivate();
                HumanCache.BladeHitRight.Deactivate();
                if (!_attackRelease)
                {
                    ContinueAnimation();
                    _attackRelease = true;
                }
            }
        }

        private void ContinueAnimation()
        {
            if (!_animationStopped)
                return;
            _animationStopped = false;
            Cache.PhotonView.RPC("ContinueAnimationRPC", PhotonTargets.All, new object[0]);
        }

        [RPC]
        public void ContinueAnimationRPC(PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            foreach (AnimationState animation in Cache.Animation)
            {
                animation.speed = 1f;
            }
            CustomAnimationSpeed();
            string animationName = GetCurrentAnimation();
            if (animationName != "")
                PlayAnimation(animationName);
        }

        private void PauseAnimation()
        {
            if (_animationStopped)
                return;
            _animationStopped = true;
            Cache.PhotonView.RPC("PauseAnimationRPC", PhotonTargets.All, new object[0]);
        }

        [RPC]
        public void PauseAnimationRPC(PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            foreach (AnimationState animation in Cache.Animation)
                animation.speed = 0f;
        }

        private void CustomAnimationSpeed()
        {
            Cache.Animation["attack5"].speed = 1.85f;
            Cache.Animation["changeBlade"].speed = 1.2f;
            Cache.Animation["air_release"].speed = 0.6f;
            Cache.Animation["changeBlade_air"].speed = 0.8f;
            Cache.Animation["AHSS_gun_reload_both"].speed = 0.38f;
            Cache.Animation["AHSS_gun_reload_both_air"].speed = 0.5f;
            Cache.Animation["AHSS_gun_reload_l"].speed = 0.4f;
            Cache.Animation["AHSS_gun_reload_l_air"].speed = 0.5f;
            Cache.Animation["AHSS_gun_reload_r"].speed = 0.4f;
            Cache.Animation["AHSS_gun_reload_r_air"].speed = 0.5f;
        }

        private bool HasHook()
        {
            return HookLeft.HasHook() || HookRight.HasHook();
        }

        private bool IsHookedAny()
        {
            return IsHookedLeft() || IsHookedRight();
        }

        private bool IsHookedLeft()
        {
            return HookLeft.IsHooked();
        }

        private bool IsHookedRight()
        {
            return HookRight.IsHooked();
        }

        private bool IsFrontGrounded()
        {
            return CheckRaycastIgnoreTriggers(Cache.Transform.position + Cache.Transform.up * 1f, Cache.Transform.forward, 1f, GroundMask.value);
        }

        private bool IsPressDirectionTowardsHero()
        {
            if (!HasDirection)
                return false;
            return (Mathf.Abs(Mathf.DeltaAngle(TargetAngle, Cache.Transform.rotation.eulerAngles.y)) < 45f);
        }

        private bool IsUpFrontGrounded()
        {
            return CheckRaycastIgnoreTriggers(Cache.Transform.position + Cache.Transform.up * 3f, Cache.Transform.forward, 1.2f, GroundMask.value);
        }

        public bool IsFiringThunderspear()
        {
            return Setup.Weapon == HumanWeapon.Thunderspear && (Cache.Animation.IsPlaying("AHSS_shoot_r") || Cache.Animation.IsPlaying("AHSS_shoot_l"));
        }

        private void UseGas(float amount)
        {
            CurrentGas -= amount;
            CurrentGas = Mathf.Max(CurrentGas, 0f);
        }

        private void ToggleBladeTrails(bool toggle, float fadeTime = 0f)
        {
            if (toggle)
            {
                if (SettingsManager.GraphicsSettings.WeaponTrailEnabled.Value)
                {
                    Setup.LeftTrail1.Activate();
                    Setup.LeftTrail2.Activate();
                    Setup.RightTrail1.Activate();
                    Setup.RightTrail2.Activate();
                }
            }
            else
            {
                if (fadeTime == 0f)
                {
                    Setup.LeftTrail1.Deactivate();
                    Setup.LeftTrail2.Deactivate();
                    Setup.RightTrail1.Deactivate();
                    Setup.RightTrail2.Deactivate();
                }
                else
                {
                    Setup.LeftTrail1.StopSmoothly(fadeTime);
                    Setup.LeftTrail2.StopSmoothly(fadeTime);
                    Setup.RightTrail1.StopSmoothly(fadeTime);
                    Setup.RightTrail2.StopSmoothly(fadeTime);
                }
            }
        }

        protected override string GetFootstepAudio(int phase)
        {
            return phase == 0 ? HumanSounds.Footstep1 : HumanSounds.Footstep2;
        }

        protected override int GetFootstepPhase()
        {
            if (Cache.Animation.IsPlaying(HumanAnimations.Run))
            {
                float time = Cache.Animation[HumanAnimations.Run].normalizedTime % 1f;
                return (time >= 0.1f && time < 0.6f) ? 1 : 0;
            }
            else if (Cache.Animation.IsPlaying(HumanAnimations.RunBuffed))
            {
                float time = Cache.Animation[HumanAnimations.RunBuffed].normalizedTime % 1f;
                return (time >= 0.1f && time < 0.6f) ? 1 : 0;
            }
            return _stepPhase;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (HumanCache.GunHit != null)
                Destroy(HumanCache.GunHit.gameObject);
            Setup.DeleteDie();
        }

        public HumanState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state == HumanState.AirDodge || _state == HumanState.GroundDodge)
                    _dashTimeLeft = 0f;
                _state = value;
            }
        }
    }

    public enum HumanState
    {
        Idle,
        Attack,
        GroundDodge,
        AirDodge,
        Reload,
        Refill,
        Die,
        Grab,
        EmoteAction,
        SpecialAttack,
        SpecialAction,
        Slide,
        Run,
        Land,
        MountingHorse,
        Stun
    }

    public enum HumanMountState
    {
        None,
        Horse,
        MapObject
    }

    public enum HumanWeapon
    {
        Blade,
        Gun,
        Thunderspear
    }

    public enum HumanDashDirection
    {
        None,
        Forward,
        Back,
        Left,
        Right
    }
}

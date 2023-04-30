using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Utility;
using Controllers;
using SimpleJSONFixed;
using Effects;
using UI;
using Settings;
using System.Collections;

namespace Characters
{
    class BaseShifter: BaseTitan
    {
        protected override int DefaultMaxHealth => 1000;
        protected override float DefaultRunSpeed => 80f;
        protected override float DefaultWalkSpeed => 20f;
        protected override float DefaultRotateSpeed => 10f;
        protected override float DefaultJumpForce => 150f;
        protected override float SizeMultiplier => 3f;
        public override float CrippleTime => 3.5f;
        protected bool _needRoar = true;
        public bool TransformingToHuman;
        public float PreviousHumanGas;
        public BaseUseable PreviousHumanWeapon;

        protected override void Start()
        {
            _inGameManager.Shifters.Add(this);
            base.Start();
            if (IsMine())
            {
                EffectSpawner.Spawn(EffectPrefabs.ShifterThunder, BaseTitanCache.Neck.position, Quaternion.identity, Size);
                PlaySound(ShifterSounds.Thunder);
            }
        }

        public override void Kick()
        {
            Attack(ShifterAttacks.AttackKick);
        }

        [RPC]
        public void MarkTransformingRPC(PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            TransformingToHuman = true;
        }

        public void Init(bool ai, string team, JSONNode data, float liveTime)
        {
            if (ai)
            {
                var controller = gameObject.AddComponent<BaseTitanAIController>();
                controller.Init(data);
                Name = data["Name"].Value;
            }
            else
            {
                gameObject.AddComponent<ShifterPlayerController>();
                if (liveTime > 0f)
                    StartCoroutine(WaitAndBecomeHuman(liveTime));
            }
            base.Init(ai, team, data);
        }

        protected IEnumerator WaitAndBecomeHuman(float time)
        {
            yield return new WaitForSeconds(time);
            Cache.PhotonView.RPC("MarkTransformingRPC", PhotonTargets.AllBuffered, new object[0]);
            Cache.PhotonView.RPC("MarkDeadRPC", PhotonTargets.AllBuffered, new object[0]);
            StartCoroutine(WaitAndDie());
            yield return new WaitForSeconds(2f);
            _inGameManager.SpawnPlayerAt(false, BaseTitanCache.Neck.position);
            StartCoroutine(((Human)_inGameManager.CurrentCharacter).WaitAndTransformFromShifter(PreviousHumanGas, PreviousHumanWeapon));
        }

        protected override void Awake()
        {
            base.Awake();
        }

        [RPC]
        public override void GetHitRPC(int viewId, string name, int damage, string type, string collider)
        {
            if (Dead)
                return;
            if (type == "CannonBall")
            {
                base.GetHitRPC(viewId, name, damage, type, collider);
                return;
            }
            var settings = SettingsManager.InGameCurrent.Titan;
            if (settings.TitanArmorEnabled.Value)
            {
                if (damage < settings.TitanArmor.Value)
                    damage = 0;
            }
            if (type == "Stun")
            {
                Stun();
                var killer = Util.FindCharacterByViewId(viewId);
                if (killer != null)
                {
                    Vector3 direction = killer.Cache.Transform.position - Cache.Transform.position;
                    direction.y = 0f;
                    Cache.Transform.forward = direction.normalized;
                }
                base.GetHitRPC(viewId, name, damage, type, collider);
            }
            else if (BaseTitanCache.EyesHurtbox != null && collider == BaseTitanCache.EyesHurtbox.name)
                Blind();
            else if (BaseTitanCache.LegLHurtbox != null && (collider == BaseTitanCache.LegLHurtbox.name || collider == BaseTitanCache.LegRHurtbox.name))
                Cripple();
            else if (collider == BaseTitanCache.NapeHurtbox.name)
                base.GetHitRPC(viewId, name, damage, type, collider);
        }

        public override void OnHit(BaseHitbox hitbox, BaseCharacter victim, Collider collider, string type, bool firstHit)
        {
            int damage = 100;
            if (CustomDamageEnabled)
                damage = CustomDamage;
            if (victim is BaseTitan)
            {
                if (firstHit)
                {
                    EffectSpawner.Spawn(EffectPrefabs.PunchHit, hitbox.transform.position, Quaternion.identity);
                    if (!victim.Dead)
                    {
                        if (IsMainCharacter())
                            ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                        victim.GetHit(this, damage, "Stun", collider.name);
                    }
                }
            }
            else
            {
                if (!victim.Dead)
                {
                    if (IsMainCharacter())
                        ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                    victim.GetHit(this, damage, type, collider.name);
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsMine())
            {
                if (_needRoar && Grounded && CanAction())
                {
                    Emote("Roar");
                    _needRoar = false;
                }
            }
        }
    }
}

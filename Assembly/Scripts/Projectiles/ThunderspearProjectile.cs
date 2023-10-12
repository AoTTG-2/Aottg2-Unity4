using Settings;
using UnityEngine;
using Photon;
using Characters;
using System.Collections.Generic;
using System.Collections;
using Effects;
using ApplicationManagers;
using GameManagers;
using UI;
using Utility;
using CustomLogic;

namespace Projectiles
{
    class ThunderspearProjectile: BaseProjectile
    {
        Color _color;
        float _radius;
        public Vector3 InitialPlayerVelocity;
        Vector3 _lastPosition;
        static LayerMask _collideMask = PhysicsLayer.GetMask(PhysicsLayer.MapObjectAll, PhysicsLayer.MapObjectEntities, PhysicsLayer.MapObjectProjectiles,
            PhysicsLayer.TitanPushbox);
        static LayerMask _blockMask = PhysicsLayer.GetMask(PhysicsLayer.MapObjectAll, PhysicsLayer.MapObjectEntities, PhysicsLayer.MapObjectProjectiles,
            PhysicsLayer.TitanPushbox, PhysicsLayer.Human);

        protected override void SetupSettings(object[] settings)
        {
            _radius = (float)settings[0];
            _color = (Color)settings[1];
            _lastPosition = transform.position;
        }

        protected override void RegisterObjects()
        {
            var trail = transform.Find("Trail").GetComponent<ParticleSystem>();
            var flame = transform.Find("Flame").GetComponent<ParticleSystem>();
            var model = transform.Find("ThunderspearModel").gameObject;
            _hideObjects.Add(flame.gameObject);
            _hideObjects.Add(model);
            if (SettingsManager.AbilitySettings.ShowBombColors.Value)
            {
                trail.startColor = _color;
                flame.startColor = _color;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (photonView.isMine && !Disabled)
            {
                if (CharacterData.HumanWeaponInfo["Thunderspear"]["Ricochet"].AsBool)
                    rigidbody.velocity = rigidbody.velocity.normalized * _velocity.magnitude * CharacterData.HumanWeaponInfo["Thunderspear"]["RicochetSpeed"].AsFloat;
                else
                {
                    Explode();
                    _rigidbody.velocity = Vector3.zero;
                }
            }
        }

        protected override void OnExceedLiveTime()
        {
            Explode();
        }

        public void Explode()
        {
            if (!Disabled)
            {
                float effectRadius = _radius * 4f;
                if (SettingsManager.InGameCurrent.Misc.ThunderspearPVP.Value)
                    effectRadius = _radius * 2f;
                EffectSpawner.Spawn(EffectPrefabs.ThunderspearExplode, transform.position, transform.rotation, effectRadius, true, new object[] { _color });
                StunMyHuman();
                KillPlayersInRadius(_radius);
                KillTitansInRadius(_radius);
                DestroySelf();
            }
        }

        void StunMyHuman()
        {
            if (_owner == null || !(_owner is Human) || !_owner.IsMainCharacter())
                return;
            if (SettingsManager.InGameCurrent.Misc.ThunderspearPVP.Value)
                return;
            float radius = CharacterData.HumanWeaponInfo["Thunderspear"]["StunBlockRadius"].AsFloat;
            float range = CharacterData.HumanWeaponInfo["Thunderspear"]["StunRange"].AsFloat;
            Vector3 direction = _owner.Cache.Transform.position - transform.position;
            RaycastHit hit;
            if (Vector3.Distance(_owner.Cache.Transform.position, transform.position) < range)
            {
                if (Physics.SphereCast(transform.position, radius, direction.normalized, out hit, range, _blockMask))
                {
                    if (hit.collider.transform.root.gameObject == _owner.gameObject)
                        ((Human)_owner).GetStunnedByTS(transform.position);
                }
            }
        }

        void KillTitansInRadius(float radius)
        {
            var position = transform.position;
            var colliders = Physics.OverlapSphere(position, radius, PhysicsLayer.GetMask(PhysicsLayer.Hurtbox));
            foreach (var collider in colliders)
            {
                var titan = collider.transform.root.gameObject.GetComponent<BaseTitan>();
                var handler = collider.gameObject.GetComponent<CustomLogicCollisionHandler>();
                if (handler != null)
                {
                    handler.GetHit(_owner, "Thunderspear", 100, "Thunderspear");
                    return;
                }
                if (titan != null && titan != _owner && !TeamInfo.SameTeam(titan, _team) && !titan.Dead)
                {
                    if (collider == titan.BaseTitanCache.NapeHurtbox && CheckTitanNapeAngle(position, titan.BaseTitanCache.Head))
                    {
                        if (_owner == null || !(_owner is Human))
                            titan.GetHit("Thunderspear", 100, "Thunderspear", collider.name);
                        else
                        {
                            var damage = CalculateDamage();
                            ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(damage);
                            titan.GetHit(_owner, damage, "Thunderspear", collider.name);
                        }
                    }
                }
            }
        }

        void KillPlayersInRadius(float radius)
        {
            var gameManager = (InGameManager)SceneLoader.CurrentGameManager;
            var position = transform.position;
            foreach (Human human in gameManager.Humans)
            {
                if (human == null || human.Dead)
                    continue;
                if (Vector3.Distance(human.Cache.Transform.position, position) < radius && human != _owner && !TeamInfo.SameTeam(human, _team))
                {
                    if (_owner == null || !(_owner is Human))
                        human.GetHit("", 100, "Thunderspear", "");
                    else
                        human.GetHit(_owner, CalculateDamage(), "Thunderspear", "");
                }
            }
        }

        int CalculateDamage()
        {
            int damage = Mathf.Max((int)(InitialPlayerVelocity.magnitude * 10f * 
                CharacterData.HumanWeaponInfo["Thunderspear"]["DamageMultiplier"].AsFloat), 10);
            if (_owner != null && _owner is Human)
            {
                var human = (Human)_owner;
                if (human.CustomDamageEnabled)
                    return human.CustomDamage;
            }
            return damage;
        }

        bool CheckTitanNapeAngle(Vector3 position, Transform nape)
        {
            Vector3 direction = (position - nape.position).normalized;
            return Vector3.Angle(-nape.forward, direction) < CharacterData.HumanWeaponInfo["Thunderspear"]["RestrictAngle"].AsFloat;
        }

        protected override void Update()
        {
            base.Update();
            if (_photonView.isMine)
            {
                if (rigidbody.velocity.magnitude > 0f)
                    transform.rotation = Quaternion.LookRotation(rigidbody.velocity);
            }
        }

        protected void FixedUpdate()
        {
            if (_photonView.isMine)
            {
                RaycastHit hit;
                Vector3 direction = (transform.position - _lastPosition);
                if (Physics.SphereCast(_lastPosition, 0.5f, direction.normalized, out hit, direction.magnitude, _collideMask))
                {
                    transform.position = hit.point;
                }
                _lastPosition = transform.position;
            }
        }
    }
}

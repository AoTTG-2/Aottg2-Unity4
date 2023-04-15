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

namespace Projectiles
{
    class ThunderspearProjectile: BaseProjectile
    {
        Color _color;
        float _radius;
        public Vector3 InitialPlayerVelocity;

        protected override void SetupSettings(object[] settings)
        {
            _radius = (float)settings[0];
            _color = (Color)settings[1];
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
                transform.position = collision.contacts[0].point;
                Explode(true);
            }
        }

        protected override void OnExceedLiveTime()
        {
            Explode(false);
        }

        public void Explode(bool impact = false)
        {
            if (!Disabled)
            {
                EffectSpawner.Spawn(EffectPrefabs.ThunderspearExplode, transform.position, transform.rotation, _radius * 2f, true,
                    new object[] { _color });
                KillPlayersInRadius(_radius);
                KillTitansInRadius(_radius, impact);
                DestroySelf();
            }
        }

        void KillTitansInRadius(float radius, bool impact)
        {
            var position = transform.position;
            var colliders = Physics.OverlapSphere(position, radius, PhysicsLayer.GetMask(PhysicsLayer.Hurtbox));
            foreach (var collider in colliders)
            {
                var titan = collider.transform.root.gameObject.GetComponent<BaseTitan>();
                if (titan != null && titan != _owner && !TeamInfo.SameTeam(titan, _team) && !titan.Dead)
                {
                    if (impact)
                        position -= _velocity * Time.fixedDeltaTime * 2f;
                    if (collider == titan.BaseTitanCache.NapeHurtbox && CheckTitanNapeAngle(position, titan.BaseTitanCache.NapeHurtbox.transform))
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
            return damage;
        }

        bool CheckTitanNapeAngle(Vector3 position, Transform nape)
        {
            Vector3 direction = (position - nape.position).normalized;
            return Vector3.Angle(-nape.forward, direction) < CharacterData.HumanWeaponInfo["Thunderspear"]["RestrictAngle"].AsFloat;
        }
    }
}

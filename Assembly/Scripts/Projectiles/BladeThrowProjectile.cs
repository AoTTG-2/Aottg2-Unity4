using Settings;
using UnityEngine;
using Photon;
using Characters;
using System.Collections.Generic;
using System.Collections;
using Effects;
using ApplicationManagers;
using GameManagers;
using Xft;
using UI;
using Utility;

namespace Projectiles
{
    class BladeThrowProjectile : BaseProjectile
    {
        protected override float DestroyDelay => 1.5f;
        protected XWeaponTrail _trail1;
        protected Transform _blade;

        protected override void Awake()
        {
            base.Awake();
            _blade = transform.Find("Blade");
            _trail1 = CreateTrail();
            _trail1.PointStart = _blade.Find("PointStart");
            _trail1.PointEnd = _blade.Find("PointEnd");
            _trail1.Activate();
        }

        protected XWeaponTrail CreateTrail()
        {
            var go = new GameObject();
            go.transform.parent = transform;
            var trail = go.AddComponent<XWeaponTrail>();
            trail.MyMaterial = HumanSetup.WeaponTrailMaterial;
            return trail;
        }

        protected override void RegisterObjects()
        {
            _hideObjects.Add(_blade.gameObject);
        }

        [RPC]
        public override void DisableRPC(PhotonMessageInfo info = null)
        {
            if (Disabled)
                return;
            if (info != null && info.sender != photonView.owner)
                return;
            base.DisableRPC();
            _trail1.Deactivate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (photonView.isMine && !Disabled)
            {
                var character = collision.collider.gameObject.transform.root.GetComponent<BaseCharacter>();
                if (character == null)
                {
                    EffectSpawner.Spawn(EffectPrefabs.BladeThrowHit, transform.position, Quaternion.LookRotation(_velocity));
                }
                else if (!TeamInfo.SameTeam(character, _team))
                {
                    if (character is BaseTitan)
                    {
                        EffectSpawner.Spawn(EffectPrefabs.Blood1, transform.position, Quaternion.Euler(270f, 0f, 0f));
                    }
                }
                transform.Find("BladeHit").GetComponent<AudioSource>().Play();
                CheckHurtboxes();
                DestroySelf();
            }
        }

        void CheckHurtboxes()
        {
            var radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
            var colliders = Physics.OverlapSphere(transform.position, radius, PhysicsLayer.GetMask(PhysicsLayer.Hurtbox, PhysicsLayer.Human));
            foreach (var collider in colliders)
            {
                var character = collider.transform.root.gameObject.GetComponent<BaseCharacter>();
                if (character == null || character == _owner || TeamInfo.SameTeam(character, _team) || character.Dead)
                    continue;
                if (character is BaseTitan)
                {
                    var titan = (BaseTitan)character;
                    Vector3 position = transform.position;
                    position -= _velocity * Time.fixedDeltaTime * 2f;
                    if (collider == titan.BaseTitanCache.NapeHurtbox)
                    {
                        if (!CheckTitanNapeAngle(position, titan.BaseTitanCache.NapeHurtbox.transform))
                            continue;
                        if (_owner != null && _owner is Human)
                            ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(100);
                        transform.Find("BladeHitNape").GetComponent<AudioSource>().Play();
                    }
                    if (titan.BaseTitanCache.Hurtboxes.Contains(collider))
                    {
                        if (collider == titan.BaseTitanCache.NapeHurtbox || !(titan is BasicTitan) || !((BasicTitan)titan).IsCrawler)
                        {
                            EffectSpawner.Spawn(EffectPrefabs.CriticalHit, transform.position, Quaternion.Euler(270f, 0f, 0f));
                            if (_owner == null || !(_owner is Human))
                                titan.GetHit("Blade", 100, "BladeThrow", collider.name);
                            else
                                titan.GetHit(_owner, 100, "BladeThrow", collider.name);
                        }
                    }
                }
                else
                {
                    ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(100);
                    character.GetHit(_owner, 100, "BladeThrow", collider.name);
                }
            }
        }

        bool CheckTitanNapeAngle(Vector3 position, Transform nape)
        {
            Vector3 direction = (position - nape.position).normalized;
            return Vector3.Angle(-nape.forward, direction) < CharacterData.HumanWeaponInfo["Blade"]["RestrictAngle"].AsFloat;
        }

        void OnDestroy()
        {
            _trail1.Deactivate();
        }

        protected override void Update()
        {
            base.Update();
            if (!Disabled)
            {
                _trail1.update();
                float speed = Mathf.Max(rigidbody.velocity.magnitude, 80f);
                float rotateSpeed = 1600f * speed;
                _blade.RotateAround(_blade.position, _blade.right, Time.deltaTime * rotateSpeed);
            }
        }

        protected void LateUpdate()
        {
            if (!Disabled)
                _trail1.lateUpdate();
        }
    }
}

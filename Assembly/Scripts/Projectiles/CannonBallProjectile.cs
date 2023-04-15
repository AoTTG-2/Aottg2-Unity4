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

namespace Projectiles
{
    class CannonBallProjectile: BaseProjectile
    {
        protected override void RegisterObjects()
        {
            var model = transform.Find("CannonBallModel").gameObject;
            _hideObjects.Add(model);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (photonView.isMine && !Disabled)
            {
                var character = collision.collider.gameObject.transform.root.GetComponent<BaseCharacter>();
                if (character != null && !TeamInfo.SameTeam(character, _team))
                {
                    if (_owner == null || !(_owner is Human))
                        character.GetHit("CannonBall", 100, "CannonBall", collision.collider.name);
                    else
                    {
                        ((InGameMenu)UIManager.CurrentMenu).ShowKillScore(100);
                        character.GetHit(_owner, 100, "CannonBall", collision.collider.name);
                    }
                }
                EffectSpawner.Spawn(EffectPrefabs.Boom4, transform.position, Quaternion.LookRotation(_velocity), 0.5f);
                DestroySelf();
            }
        }
    }
}

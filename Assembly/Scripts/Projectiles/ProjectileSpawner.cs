using ApplicationManagers;
using GameManagers;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    class ProjectileSpawner: MonoBehaviour
    {
        public static BaseProjectile Spawn(string name, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 gravity, float liveTime,
            int charViewId, string team, object[] settings = null)
        {
            GameObject go = PhotonNetwork.Instantiate(name, position, rotation, 0);
            BaseProjectile projectile;
            if (name == ProjectilePrefabs.Thunderspear)
                projectile = go.GetComponent<ThunderspearProjectile>();
            else
                projectile = go.GetComponent<BaseProjectile>();
            projectile.Setup(liveTime, velocity, gravity, charViewId, team, settings);
            return projectile;
        }
    }
}

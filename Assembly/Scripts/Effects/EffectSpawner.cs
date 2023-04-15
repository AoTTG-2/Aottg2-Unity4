using ApplicationManagers;
using GameManagers;
using Settings;
using UnityEngine;
using Utility;

namespace Effects
{
    class EffectSpawner
    {
        public static void Spawn(string name, Vector3 position, Quaternion rotation, float scale = 1f, bool scaleSize = true, object[] settings = null)
        {
            RPCManager.PhotonView.RPC("SpawnEffectRPC", PhotonTargets.All, new object[] { name, position, rotation, scale, scaleSize, settings });
        }

        public static void OnSpawnEffectRPC(string name, Vector3 position, Quaternion rotation, float scale, bool scaleSize, object[] settings, PhotonMessageInfo info)
        {
            GameObject go;
            if (name.StartsWith("RCAsset/"))
                go = AssetBundleManager.InstantiateAsset<GameObject>(name.Substring(8), position, rotation);
            else
                go = ResourceManager.InstantiateAsset<GameObject>(name, position, rotation);
            BaseEffect effect;
            if (name == EffectPrefabs.ThunderspearExplode)
            {
                effect = go.AddComponent<ThunderspearExplodeEffect>();
                effect.Setup(info.sender, 10f, settings);
            }
            else
            {
                effect = go.AddComponent<BaseEffect>();
                effect.Setup(info.sender, 10f, settings);
            }
            ScaleEffect(go.transform, scale, scaleSize);
        }

        private static void ScaleEffect(Transform transform, float scale, bool scaleSize)
        {
            transform.localScale = new Vector3(scale, scale, scale);
            if (!scaleSize)
                return;
            foreach (ParticleSystem system in transform.GetComponentsInChildren<ParticleSystem>())
            {
                system.startSpeed *= scale;
                system.startSize *= scale;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ApplicationManagers
{
    class ResourceManager : MonoBehaviour
    {
        static Dictionary<string, Object> _cache = new Dictionary<string, Object>();

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static Object LoadAsset(string name, bool cached = false)
        {
            if (cached)
            {
                if (!_cache.ContainsKey(name))
                    _cache.Add(name, Resources.Load(name));
                return _cache[name];
            }
            return Resources.Load(name);
        }

        public static T InstantiateAsset<T>(string name, bool cached = false) where T : Object
        {
            return (T)Instantiate(LoadAsset(name, cached));
        }

        public static T InstantiateAsset<T>(string name, Vector3 position, Quaternion rotation, bool cached = false) where T : Object
        {
            return (T)Instantiate(LoadAsset(name, cached), position, rotation);
        }
    }
}
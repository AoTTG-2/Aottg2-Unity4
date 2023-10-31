using UnityEngine;
using System.Collections;
using Utility;
using System.Collections.Generic;
using Events;
using System.IO;

namespace ApplicationManagers
{
    /// <summary>
    /// Handles loading the main asset bundle and instantiating assets from it.
    /// The game currently relies on the main asset bundle rather than Resources for most prefabs.
    /// </summary>
    public class AssetBundleManager : MonoBehaviour
    {
        public static AssetBundle MainAssetBundle;
        public static AssetBundle MusicAssetBundle;
        public static AssetBundleStatus Status = AssetBundleStatus.Loading;
        public static bool CloseFailureBox = false;
        private static AssetBundleManager _instance;
        private static Dictionary<string, Object> _cache = new Dictionary<string, Object>();

        // consts
        private static readonly string RootDataPath = Application.dataPath;
        private static readonly string MainAssetBundlePath = RootDataPath + "/MainAssets.unity3d";
        private static readonly string MusicAssetBundlePath = RootDataPath + "/MusicAssets.unity3d";

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
        }

        public static void LoadAssetBundle()
        {
            _instance.StartCoroutine(_instance.LoadAssetBundleCoroutine());
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static Object LoadAsset(string name, bool cached = false)
        {
            if (cached)
            {
                if (!_cache.ContainsKey(name))
                    _cache.Add(name, MainAssetBundle.Load(name));
                return _cache[name];
            }
            return MainAssetBundle.Load(name);
        }

        public static Object LoadMusic(string name, bool cached = false)
        {
            if (cached)
            {
                if (!_cache.ContainsKey(name))
                    _cache.Add(name, MusicAssetBundle.Load(name));
                return _cache[name];
            }
            return MusicAssetBundle.Load(name);
        }

        public static Object TryLoadAsset(string name, bool cached = false)
        {
            try
            {
                return LoadAsset(name, cached);
            }
            catch (System.Exception e)
            {
                Debug.Log(string.Format("Error loading asset from asset bundle: {0}, {1}", name, e.Message));
            }
            return null;
        }

        public static string LoadText(string name)
        {
            return ((TextAsset)MainAssetBundle.Load(name)).text;
        }

        public static string TryLoadText(string name)
        {
            try
            {
                return LoadText(name);
            }
            catch (System.Exception e)
            {
                Debug.Log(string.Format("Error loading text from asset bundle: {0}, {1}", name, e.Message));
            }
            return string.Empty;
        }

        public static T InstantiateAsset<T>(string name, bool cached = false) where T : Object
        {
            return (T)Instantiate(LoadAsset(name, cached));
        }

        public static T InstantiateAsset<T>(string name, Vector3 position, Quaternion rotation, bool cached = false) where T : Object
        {
            return (T)Instantiate(LoadAsset(name, cached), position, rotation);
        }

        IEnumerator LoadAssetBundleCoroutine()
        {
            Status = AssetBundleStatus.Loading;
            while (!Caching.ready)
                yield return null;
            MainAssetBundle = AssetBundle.CreateFromFile(MainAssetBundlePath);
            if (MainAssetBundle != null)
                MusicAssetBundle = AssetBundle.CreateFromFile(MusicAssetBundlePath);
            if (MainAssetBundle == null  || MusicAssetBundle == null)
            {
                Debug.Log("Failed to load asset bundle using CreateFromFile, trying CreateFromMemory");
                var request1 = TryCreateRequest(MainAssetBundlePath);
                var request2 = TryCreateRequest(MusicAssetBundlePath);
                if (request1 == null || request2 == null)
                {
                    Status = AssetBundleStatus.Failed;
                    yield break;
                }
                yield return request1;
                yield return request2;
                MainAssetBundle = request1.assetBundle;
                MusicAssetBundle = request2.assetBundle;
                if (MainAssetBundle == null || MusicAssetBundle == null)
                {
                    Debug.Log("Failed to load asset bundle using CreateFromMemory");
                    Status = AssetBundleStatus.Failed;
                }
                else
                    Status = AssetBundleStatus.Ready;
            }
            else
                Status = AssetBundleStatus.Ready;
        }

        private AssetBundleCreateRequest TryCreateRequest(string path)
        {
            try
            {
                return AssetBundle.CreateFromMemory(File.ReadAllBytes(path));
            }
            catch (System.Exception e)
            {
                Debug.Log("Failed to create assetbundle from memory: " + e.Message);
                return null;
            }
        }
    }

    public enum AssetBundleStatus
    {
        Loading,
        Ready,
        Failed
    }
}
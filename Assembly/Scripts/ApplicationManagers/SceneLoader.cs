using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using GameManagers;
using Events;
using Cameras;
using Map;
using Projectiles;
using Characters;

namespace ApplicationManagers
{
    /// <summary>
    /// Manager used by other classes to load and setup scenes with proper game managers, maps, and cameras.
    /// </summary>
    class SceneLoader : MonoBehaviour
    {
        static SceneLoader _instance;
        public static SceneName SceneName = SceneName.Startup;
        public static BaseGameManager CurrentGameManager;
        public static BaseCamera CurrentCamera;

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
            LoadScene(SceneName.Startup);
        }

        public static void LoadScene(SceneName sceneName)
        {
            EventManager.InvokePreLoadScene(sceneName);
            SceneName = sceneName;
            ClothFactory.DisposeAllObjects();
            ClothFactory.ClearClothCache();
            ResourceManager.ClearCache();
            AssetBundleManager.ClearCache();
            if (sceneName == SceneName.InGame)
                PhotonNetwork.LoadLevel(9);
            else
                Application.LoadLevel(9);
            CharacterData.Init(); // remove this after testing is done
        }

        private static void CreateGameManager()
        {
            if (CurrentGameManager != null)
                Debug.Log("Warning: game manager already exists.");
            if (SceneName == SceneName.MainMenu)
                CurrentGameManager = Util.CreateObj<MainMenuGameManager>();
            else if (SceneName == SceneName.InGame)
                CurrentGameManager = Util.CreateObj<InGameManager>();
            else if (SceneName == SceneName.CharacterEditor)
                CurrentGameManager = Util.CreateObj<CharacterEditorGameManager>();
            else if (SceneName == SceneName.MapEditor)
                CurrentGameManager = Util.CreateObj<MapEditorGameManager>();
        }

        private static void CreateCamera()
        {
            if (CurrentCamera != null)
                Debug.Log("Warning: Camera already exists.");
            if (SceneName == SceneName.Startup)
                return;
            var go = AssetBundleManager.InstantiateAsset<GameObject>("MainCamera");
            if (SceneName == SceneName.InGame)
                CurrentCamera = go.AddComponent<InGameCamera>();
            else if (SceneName == SceneName.MapEditor)
                CurrentCamera = go.AddComponent<MapEditorCamera>();
            else if (SceneName == SceneName.CharacterEditor)
                CurrentCamera = go.AddComponent<CharacterEditorCamera>();
            else if (SceneName == SceneName.Test)
                CurrentCamera = go.AddComponent<TestCamera>();
            else
            {
                CurrentCamera = go.AddComponent<StaticCamera>();
                CurrentCamera.Camera.nearClipPlane = 0.3f;
            }
        }

        private void OnLevelWasLoaded(int level)
        {
            if (level != 9)
                return;
            foreach (GameObject obj in FindObjectsOfType(typeof(GameObject)))
            {
                if (obj.GetComponent<DontDestroyOnLoadTag>() == null && obj.name != "PhotonMono")
                    Destroy(obj);
            }
            CreateGameManager();
            CreateCamera();
            EventManager.InvokeLoadScene(SceneName);
        }
    }

    public enum SceneName
    {
        Startup,
        MainMenu,
        InGame,
        MapEditor,
        CharacterEditor,
        Test
    }
}

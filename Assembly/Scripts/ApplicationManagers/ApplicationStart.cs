using UnityEngine;
using Utility;
using Settings;
using UI;
using Weather;
using System.Collections;
using GameProgress;
using Map;
using GameManagers;
using Events;
using Characters;
using CustomLogic;
using CustomSkins;
using Anticheat;
using Photon;

namespace ApplicationManagers
{
    /// <summary>
    /// Application entry point. Runs on main scene load, and handles loading every other manager.
    /// </summary>
    class ApplicationStart : UnityEngine.MonoBehaviour
    {
        private static bool _firstLaunch = true;
        private static ApplicationStart _instance;
        private static Texture2D _textureBackgroundBlack;
        private static Texture2D _textureBackgroundBlue;
        private static Texture2D _textureBackgroundDarkBlue;

        public static void Init()
        {
            if (_firstLaunch)
            {
                _firstLaunch = false;
                _instance = SingletonFactory.CreateSingleton(_instance);
                Start();
            }
        }

        private static void Start()
        {
            DebugConsole.Init();
            ApplicationConfig.Init();
            AnticheatManager.Init();
            PhysicsLayer.Init();
            MaterialCache.Init();
            EventManager.Init();
            SettingsManager.Init();
            FullscreenHandler.Init();
            UIManager.Init();
            AssetBundleManager.Init();
            SnapshotManager.Init();
            CursorManager.Init();
            WeatherManager.Init();
            GameProgressManager.Init();
            SceneLoader.Init();
            MapManager.Init();
            CustomLogicManager.Init();
            ChatManager.Init();
            PastebinLoader.Init();
            MusicManager.Init();
            CustomSerialization.Init();
            if (ApplicationConfig.DevelopmentMode)
            {
                DebugTesting.Init();
                DebugTesting.RunTests();
            }
            _instance.StartCoroutine(_instance.Load());
        }

        private IEnumerator Load()
        {
            AssetBundleManager.LoadAssetBundle();
            PastebinLoader.LoadPastebin();
            while (AssetBundleManager.Status == AssetBundleStatus.Loading || PastebinLoader.Status == PastebinStatus.Loading)
                yield return null;
            EventManager.InvokeFinishInit();
            HumanSetup.Init();
            BasicTitanSetup.Init();
            CharacterData.Init();
            SceneLoader.LoadScene(SceneName.MainMenu);
            if (ApplicationConfig.DevelopmentMode)
                DebugTesting.RunLateTests();
        }

        private void OnGUI()
        {
            if (SceneLoader.SceneName == SceneName.Startup || SceneLoader.SceneName == SceneName.MainMenu)
            {
                if (_textureBackgroundBlack == null)
                {
                    _textureBackgroundBlack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    _textureBackgroundBlack.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
                    _textureBackgroundBlack.Apply();
                }
                if (_textureBackgroundBlue == null)
                {
                    _textureBackgroundBlue = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    _textureBackgroundBlue.SetPixel(0, 0, new Color(0.08f, 0.3f, 0.4f, 1f));
                    _textureBackgroundBlue.Apply();
                }
                if (_textureBackgroundDarkBlue == null)
                {
                    _textureBackgroundDarkBlue = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    _textureBackgroundDarkBlue.SetPixel(0, 0, new Color(0.125f, 0.164f, 0.266f, 1f));
                    _textureBackgroundDarkBlue.Apply();
                }
                LegacyPopupTemplate popup = new LegacyPopupTemplate(_textureBackgroundDarkBlue, _textureBackgroundBlue, new Color(1f, 1f, 1f, 1f),
                        Screen.width / 2f, Screen.height / 2f, 230f, 140f, 2f);
                DrawBackgroundIfLoading(_textureBackgroundBlack);
                if (AssetBundleManager.Status == AssetBundleStatus.Loading)
                {
                    popup.DrawPopup("Downloading asset bundle", 190f, 22f);
                }
                else if (AssetBundleManager.Status == AssetBundleStatus.Failed && !AssetBundleManager.CloseFailureBox)
                {
                    if (popup.DrawPopupWithButton("Failed to load asset bundle, check your internet connection.", 190f, 44f, "Continue", 80f, 25f))
                        AssetBundleManager.CloseFailureBox = true;
                }
            }
        }

        void DrawBackgroundIfLoading(Texture2D texture)
        {
            if (AssetBundleManager.Status == AssetBundleStatus.Loading)
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texture);
        }
    }
}
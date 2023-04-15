using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using Utility;
using Settings;
using UI;
using System.Collections.Generic;

namespace ApplicationManagers
{
    /// <summary>
    /// Used to handle changes between fullscreen and windowed, or to handle exclusive-fullscreen behaviors.
    /// </summary>
    class FullscreenHandler : MonoBehaviour
    {
        static FullscreenHandler _instance;
        static bool _exclusiveFullscreen;
        static Resolution _resolution;

        // windows minimize functions
        [DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
        private static extern int GetActiveWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(int hWnd, int nCmdShow);

        // consts
        static readonly string RootPath = Application.dataPath + "/FullscreenFix";
        static readonly string BorderlessPath = RootPath + "/mainDataBorderless";
        static readonly string ExclusivePath = RootPath + "/mainDataExclusive";
        static readonly string MainDataPath = Application.dataPath + "/mainData";

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
            _exclusiveFullscreen = SettingsManager.GraphicsSettings.FullScreenMode.Value == (int)FullScreenLevel.Exclusive;
        }

        public static void Apply(int resolutionIndex, FullScreenLevel fullscreenLevel)
        {
            var resolutions = GetResolutions();
            _resolution = resolutions[resolutionIndex];
            SetFullscreen(fullscreenLevel != FullScreenLevel.Windowed);
        }

        public static int SanitizeResolutionSetting(int resolutionIndex)
        {
            var resolutions = GetResolutions();
            if (resolutionIndex >= resolutions.Count)
                resolutionIndex = -1;
            if (resolutionIndex == -1)
                resolutionIndex = resolutions.Count - 1;
            return resolutionIndex;
        }

        public static string[] GetResolutionOptions()
        {
            List<string> options = new List<string>();
            var resolutions = GetResolutions();
            foreach (var resolution in resolutions)
                options.Add(resolution.width.ToString() + " x " + resolution.height.ToString());
            return options.ToArray();
        }

        static List<Resolution> GetResolutions()
        {
            List<Resolution> resolutions = new List<Resolution>();
            foreach (var resolution in Screen.resolutions)
            {
                resolutions.Add(resolution);
            }
            if (resolutions.Count == 0)
            {
                var resolution = new Resolution();
                resolution.width = 800;
                resolution.height = 600;
                resolutions.Add(resolution);
            }
            return resolutions;
        }

        static void SetFullscreen(bool fullscreen)
        {
            if (fullscreen)
                Screen.SetResolution(_resolution.width, _resolution.height, true);
            else
                Screen.SetResolution(_resolution.width, _resolution.height, false);
            CursorManager.RefreshCursorLock();
            if (UIManager.CurrentMenu != null)
                UIManager.CurrentMenu.ApplyScale(SceneLoader.SceneName);
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (!Supported())
                return;
            if (_exclusiveFullscreen)
            {
                if (hasFocus && SettingsManager.GraphicsSettings != null)
                    SetFullscreen(!IsWindowed());
                else
                {
                    // need to manually minimize when alt-tabbing on exclusive-fullscreen
                    SetFullscreen(false);
                    int handle = GetActiveWindow();
                    ShowWindow(handle, 2);
                }
            }
            CursorManager.RefreshCursorLock();
        }

        public static void SetMainData(bool trueFullscreen)
        {
            if (!Supported())
                return;
            try
            {
                if (trueFullscreen)
                    File.Copy(ExclusivePath, MainDataPath, true);
                else
                    File.Copy(BorderlessPath, MainDataPath, true);
            }
            catch (Exception ex)
            {
                Debug.Log("FullscreenHandler error setting main data: " + ex.Message);
            }
        }

        static bool IsWindowed()
        {
            return SettingsManager.GraphicsSettings.FullScreenMode.Value == (int)FullScreenLevel.Windowed;
        }

        static bool Supported()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer;
        }
    }
}
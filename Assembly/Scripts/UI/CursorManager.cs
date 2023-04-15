using UnityEngine;
using Utility;
using System.Collections.Generic;
using System.IO;
using Settings;
using System.Linq;
using SimpleJSONFixed;
using UnityEngine.UI;
using System.Collections;
using ApplicationManagers;
using System;
using Events;
using GameManagers;
using Cameras;
using Characters;
using CustomLogic;

namespace UI
{
    class CursorManager : MonoBehaviour
    {
        public static CursorState State;
        private static CursorManager _instance;
        private static Texture2D _cursorPointer;
        private static Dictionary<CrosshairStyle, Texture2D> _crosshairs = new Dictionary<CrosshairStyle, Texture2D>();
        private bool _ready;
        private bool _crosshairWhite = true;
        private bool _lastCrosshairWhite = false;
        private string _crosshairText = string.Empty;
        private bool _forceNextCrosshairUpdate = false;
        private CrosshairStyle _lastCrosshairStyle = CrosshairStyle.Default;

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
            EventManager.OnFinishInit += OnFinishInit;
        }

        public static void OnFinishInit()
        {
            _cursorPointer = (Texture2D)AssetBundleManager.MainAssetBundle.Load("CursorPointer");
            foreach (CrosshairStyle style in Enum.GetValues(typeof(CrosshairStyle)))
            {
                Texture2D crosshair = (Texture2D)AssetBundleManager.MainAssetBundle.Load("Cursor" + style.ToString());
                _crosshairs.Add(style, crosshair);
            }
            _instance._ready = true;
            SetPointer(true);
        }

        private void Update()
        {
            if (SceneLoader.SceneName == SceneName.Startup || SceneLoader.SceneName == SceneName.MainMenu || SceneLoader.SceneName == SceneName.CharacterEditor)
                SetPointer();
            else
            {
                if (UIManager.CurrentMenu == null || !(UIManager.CurrentMenu is InGameMenu))
                    return;
                var manager = (InGameManager)SceneLoader.CurrentGameManager;
                if (InGameMenu.InMenu() || !manager.IsFinishedLoading())
                    SetPointer();
                else if (manager.CurrentCharacter != null && manager.CurrentCharacter is Human && !manager.CurrentCharacter.Dead && !CustomLogicManager.Cutscene)
                    SetCrosshair();
                else
                    SetHidden();
            }
        }

        public static void RefreshCursorLock()
        {
            if (Screen.lockCursor)
            {
                Screen.lockCursor = !Screen.lockCursor;
                Screen.lockCursor = !Screen.lockCursor;
            }
        }

        public static void SetPointer(bool force = false)
        {
            if (force || State != CursorState.Pointer)
            {
                Screen.showCursor = true;
                Screen.lockCursor = false;
                State = CursorState.Pointer;
            }
        }

        public static void SetHidden(bool force = false)
        {
            if (force || State != CursorState.Hidden)
            {
                Screen.showCursor = false;
                State = CursorState.Hidden;
            }
            if (((InGameCamera)SceneLoader.CurrentCamera).CurrentCameraMode == CameraInputMode.TPS)
            {
                if (!Screen.lockCursor)
                    Screen.lockCursor = true;
            }
            else if (Screen.lockCursor)
                Screen.lockCursor = false;
        }

        public static void SetCrosshair(bool force = false)
        {
            if (force || (State != CursorState.Crosshair))
            {
                Screen.showCursor = false;
                State = CursorState.Crosshair;
            }
            if (((InGameCamera)SceneLoader.CurrentCamera).CurrentCameraMode == CameraInputMode.TPS)
            {
                if (!Screen.lockCursor)
                    Screen.lockCursor = true;
            }
            else if (Screen.lockCursor)
                Screen.lockCursor = false;
        }

        public static void SetCrosshairColor(bool white)
        {
            if (_instance._crosshairWhite != white)
            {
                _instance._crosshairWhite = white;
            }
        }

        public static void SetCrosshairText(string text)
        {
            _instance._crosshairText = text;
        }

        public static void UpdateCrosshair(RawImage crosshairImageWhite, RawImage crosshairImageRed, Text crosshairLabelWhite,
            Text crosshairLabelRed, bool force = false)
        {
            if (_instance._ready)
            {
                if (State != CursorState.Crosshair) //InGameMenu.HideCrosshair)
                {
                    if (crosshairImageRed.gameObject.activeSelf)
                        crosshairImageRed.gameObject.SetActive(false);
                    if (crosshairImageWhite.gameObject.activeSelf)
                        crosshairImageWhite.gameObject.SetActive(false);
                    _instance._forceNextCrosshairUpdate = true;
                    return;
                }
                CrosshairStyle style = (CrosshairStyle)SettingsManager.UISettings.CrosshairStyle.Value;
                if (_instance._lastCrosshairStyle != style || force || _instance._forceNextCrosshairUpdate)
                {
                    crosshairImageWhite.texture = _crosshairs[style];
                    crosshairImageRed.texture = _crosshairs[style];
                    _instance._lastCrosshairStyle = style;
                }
                if (_instance._crosshairWhite != _instance._lastCrosshairWhite || force || _instance._forceNextCrosshairUpdate)
                {
                    crosshairImageWhite.gameObject.SetActive(_instance._crosshairWhite);
                    crosshairImageRed.gameObject.SetActive(!_instance._crosshairWhite);
                    _instance._lastCrosshairWhite = _instance._crosshairWhite;
                }
                Text crosshairLabel = crosshairLabelWhite;
                RawImage crosshairImage = crosshairImageWhite;
                if (!_instance._crosshairWhite)
                {
                    crosshairLabel = crosshairLabelRed;
                    crosshairImage = crosshairImageRed;
                }
                crosshairLabel.text = _instance._crosshairText;
                Vector3 mousePosition = Input.mousePosition;
                Transform crosshairTransform = crosshairImage.transform;
                if (crosshairTransform.position != mousePosition)
                {
                    if (((InGameCamera)SceneLoader.CurrentCamera).CurrentCameraMode == CameraInputMode.TPS)
                    {
                        if (Math.Abs(crosshairTransform.position.x - mousePosition.x) > 1f || Math.Abs(crosshairTransform.position.y - mousePosition.y) > 1f)
                        {
                            crosshairTransform.position = mousePosition;
                        }
                    }
                    else
                        crosshairTransform.position = mousePosition;
                }
                _instance._forceNextCrosshairUpdate = false;
            }
        }
    }

    public enum CursorState
    {
        Pointer,
        Crosshair,
        Hidden
    }

    public enum CrosshairStyle
    {
        Default,
        Square,
        Plus,
        Target,
        Dot
    }
}

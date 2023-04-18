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
using ApplicationManagers;
using Characters;
using System.Linq;
using System.Collections.Generic;
using CustomLogic;
using Projectiles;
using static UnityEngine.UI.GridLayoutGroup;

namespace Cameras
{
    class InGameCamera : BaseCamera
    {
        public BaseCharacter _follow;
        private InGameManager _inGameManager;
        private InGameMenu _menu;
        private GeneralInputSettings _input;
        public CameraInputMode CurrentCameraMode;
        private float _cameraDistance;
        private float _heightDistance;
        private float _anchorDistance;
        private const float DistanceMultiplier = 10f;


        public void ApplyGraphicsSettings()
        {
            Camera.farClipPlane = SettingsManager.GraphicsSettings.RenderDistance.Value;
        }

        public void ApplyGeneralSettings()
        {
            _cameraDistance = SettingsManager.GeneralSettings.CameraDistance.Value + 0.3f;
            CurrentCameraMode = (CameraInputMode)SettingsManager.GeneralSettings.CameraMode.Value;
        }

        protected override void SetDefaultCameraPosition()
        {
            GameObject go = MapManager.GetRandomTag(MapTags.CameraSpawnPoint);
            if (go != null)
            {
                Cache.Transform.position = go.transform.position;
                Cache.Transform.rotation = go.transform.rotation;
            }
            else
            {
                Cache.Transform.position = Vector3.up * 100f;
                Cache.Transform.rotation = Quaternion.identity;
            }
        }

        public void SetFollow(BaseCharacter character, bool resetRotation = true)
        {
            _follow = character;
            if (_follow == null)
            {
                _menu.SetBottomHUD();
                return;
            }
            if (character is Human)
            {
                _anchorDistance = _heightDistance = 0.64f;
            }
            else if (character is BaseTitan || character is BaseShifter)
            {
                _anchorDistance = Vector3.Distance(character.GetCameraAnchor().position, character.Cache.Transform.position) * 0.25f;
                _heightDistance = Vector3.Distance(character.GetCameraAnchor().position, character.Cache.Transform.position) * 0.35f;
            }
            else
                _anchorDistance = _heightDistance = 1f;
            if (resetRotation)
                Cache.Transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            if (character is Human && character.IsMine())
                _menu.SetBottomHUD((Human)character);
            else
                _menu.SetBottomHUD();
        }

        protected override void Awake()
        {
            base.Awake();
            ApplyGraphicsSettings();
            ApplyGeneralSettings();
        }

        protected void Start()
        {
            _inGameManager = (InGameManager)SceneLoader.CurrentGameManager;
            _input = SettingsManager.InputSettings.General;
            _menu = (InGameMenu)UIManager.CurrentMenu;
        }
        
        protected void LateUpdate()
        {
            if (CustomLogicManager.Cutscene)
            {
                Camera.fieldOfView = 50f;
                Cache.Transform.position = CustomLogicManager.CutsceneCameraPosition;
                Cache.Transform.rotation = Quaternion.Euler(CustomLogicManager.CutsceneCameraRotation);
                return;
            }
            if (_follow != _inGameManager.CurrentCharacter && _inGameManager.CurrentCharacter != null)
                SetFollow(_inGameManager.CurrentCharacter);
            if (_follow == null)
                FindNextSpectate();
            if (_follow != null)
            {
                if (_follow == _inGameManager.CurrentCharacter)
                {
                    UpdateMain();
                    if (_follow.Dead)
                        _menu.SetBottomHUD();
                }
                else
                    UpdateSpectate();
                UpdateObstacles();
                if (_follow.Dead)
                    _menu.SetBottomHUD();
            }
            UpdateFOV();
        }

        private void UpdateMain()
        {

            if (_input.ChangeCamera.GetKeyDown() && !ChatManager.IsChatActive() && !InGameMenu.InMenu())
            {
                if (CurrentCameraMode == CameraInputMode.TPS)
                    CurrentCameraMode = CameraInputMode.Original;
                else if (CurrentCameraMode == CameraInputMode.Original)
                    CurrentCameraMode = CameraInputMode.TPS;
            }

            //Flare Fast Instantiating
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _follow.UseItem(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _follow.UseItem(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _follow.UseItem(2);
            }

            float offset = _cameraDistance * (200f - Camera.fieldOfView) / 150f;
            Cache.Transform.position = _follow.GetCameraAnchor().position;
            Cache.Transform.position += Vector3.up * _heightDistance;
            Cache.Transform.position -= Vector3.up * (0.6f - _cameraDistance) * 2f;
            float sensitivity = SettingsManager.GeneralSettings.MouseSpeed.Value;
            int invertY = SettingsManager.GeneralSettings.InvertMouse.Value ? -1 : 1;
            if (InGameMenu.InMenu())
                sensitivity = 0f;
            if (CurrentCameraMode == CameraInputMode.Original)
            {
                if (Input.mousePosition.x < (Screen.width * 0.4f))
                {
                    float angle = -(((Screen.width * 0.4f) - Input.mousePosition.x) / Screen.width) * 0.4f * 150f * GetSensitivityDeltaTime(sensitivity);
                    Cache.Transform.RotateAround(Cache.Transform.position, Vector3.up, angle);
                }
                else if (Input.mousePosition.x > (Screen.width * 0.6f))
                {
                    float angle = ((Input.mousePosition.x - (Screen.width * 0.6f)) / Screen.width) * 0.4f * 150f * GetSensitivityDeltaTime(sensitivity);
                    Cache.Transform.RotateAround(Cache.Transform.position, Vector3.up, angle);
                }
                float rotationX = 0.5f * (140f * (Screen.height * 0.6f - Input.mousePosition.y)) / Screen.height;
                Cache.Transform.rotation = Quaternion.Euler(rotationX, Cache.Transform.rotation.eulerAngles.y, Cache.Transform.rotation.eulerAngles.z);
                Cache.Transform.position -= Cache.Transform.forward * DistanceMultiplier * _anchorDistance * offset;
            }
            else if (CurrentCameraMode == CameraInputMode.TPS)
            {
                float inputX = Input.GetAxis("Mouse X") * 10f * sensitivity;
                float inputY = -Input.GetAxis("Mouse Y") * 10f * sensitivity * invertY;
                Cache.Transform.RotateAround(Cache.Transform.position, Vector3.up, inputX);
                float angleY = Cache.Transform.rotation.eulerAngles.x % 360f;
                float sumY = inputY + angleY;
                bool rotateUp = inputY <= 0f || ((angleY >= 260f || sumY <= 260f) && (angleY >= 80f || sumY <= 80f));
                bool rotateDown = inputY >=0f || ((angleY <= 280f || sumY >= 280f) && (angleY <= 100f || sumY >= 100f));
                if (rotateUp && rotateDown)
                    Cache.Transform.RotateAround(Cache.Transform.position, Cache.Transform.right, inputY);
                Cache.Transform.position -= Cache.Transform.forward * DistanceMultiplier * _anchorDistance * offset;
            }
            if (_cameraDistance < 0.65f)
            {
                // Cache.Transform.position += Cache.Transform.right * Mathf.Max(2f * (0.6f - _cameraDistance), 0.65f);
            }
        }

        private void UpdateSpectate()
        {
            float offset = _cameraDistance * (200f - Camera.fieldOfView) / 150f;
            Cache.Transform.rotation = Quaternion.Lerp(Cache.Transform.rotation, _follow.GetComponent<BaseMovementSync>()._correctCamera,
                Time.deltaTime * 10f);
            Cache.Transform.position = _follow.GetCameraAnchor().position;
            Cache.Transform.position += Vector3.up * _heightDistance;
            Cache.Transform.position -= Vector3.up * (0.6f - _cameraDistance) * 2f;
            Cache.Transform.position -= Cache.Transform.forward * DistanceMultiplier * _anchorDistance * offset;
            if (_inGameManager.Humans.Count > 0 && !InGameMenu.InMenu() && !ChatManager.IsChatActive())
            {
                if (_input.SpectateNextPlayer.GetKeyDown())
                {
                    int nextSpectateIndex = GetSpectateIndex() + 1;
                    if (nextSpectateIndex >= _inGameManager.Humans.Count)
                        nextSpectateIndex = 0;
                    SetFollow(GetSortedCharacters()[nextSpectateIndex]);
                }
                if (_input.SpectatePreviousPlayer.GetKeyDown())
                {
                    int nextSpectateIndex = GetSpectateIndex() - 1;
                    if (nextSpectateIndex < 0)
                        nextSpectateIndex = _inGameManager.Humans.Count - 1;
                    SetFollow(GetSortedCharacters()[nextSpectateIndex]);
                }
            }
        }

        private void UpdateObstacles()
        {
            Vector3 start = _follow.GetCameraAnchor().position;
            Vector3 direction = (start - Cache.Transform.position).normalized;
            Vector3 end = start - direction * DistanceMultiplier * _anchorDistance;
            LayerMask mask = PhysicsLayer.GetMask(PhysicsLayer.MapObjectMapObjects);
            RaycastHit hit;
            if (Physics.Linecast(start, end, out hit, mask))
                Cache.Transform.position = hit.point;
        }

        private void UpdateFOV()
        {
            if (_follow != null && _follow is Human)
            {
                float speed = _follow.Cache.Rigidbody.velocity.magnitude;
                if (speed > 10f)
                {
                    Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, Mathf.Min(100f, speed + 40f), 0.1f);
                    return;
                }
                else
                    Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, 50f, 0.1f);
            }
            else
                Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, 50f, 0.1f);
        }


        private void FindNextSpectate()
        {
            var characters = GetSortedCharacters();
            if (characters.Count > 0)
                SetFollow(characters[0]);
            else
                SetFollow(null);
        }

        private int GetSpectateIndex()
        {
            if (_follow == null)
                return -1;
            var humans = GetSortedCharacters();
            for (int i = 0; i < humans.Count; i++)
            {
                if (humans[i] == _follow)
                    return i;
            }
            return -1;
        }

        private float GetSensitivityDeltaTime(float sensitivity)
        {
            return (sensitivity * Time.deltaTime) * 62f;
        }

        private List<BaseCharacter> GetSortedCharacters()
        {
            List<BaseCharacter> characters = new List<BaseCharacter>();
            foreach (var human in _inGameManager.Humans)
            {
                if (!human.AI)
                    characters.Add(human);
            } 
            foreach (var shifter in _inGameManager.Shifters)
            {
                if (!shifter.AI)
                    characters.Add(shifter);
            }
            return characters.OrderBy(x => x.Cache.PhotonView.ownerId).ToList();
        }
    }
}

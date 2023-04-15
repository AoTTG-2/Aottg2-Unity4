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

namespace Cameras
{
    class MapEditorCamera : BaseCamera
    {
        private MapEditorInputSettings _input;
        private MapEditorSettings _settings;
        private MapEditorMenu _menu;
        private Camera _uiCamera;

        protected override void Awake()
        {
            base.Awake();
            _input = SettingsManager.InputSettings.MapEditor;
            _settings = SettingsManager.MapEditorSettings;
            ApplyGraphicsSettings();
        }

        public void ApplyGraphicsSettings()
        {
            Camera.farClipPlane = SettingsManager.MapEditorSettings.RenderDistance.Value;
        }

        protected void CreateUICamera()
        {
            _uiCamera = Util.CreateObj<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.Depth;
            _uiCamera.cullingMask = PhysicsLayer.GetMask(PhysicsLayer.UI);
            _uiCamera.depth = 1;
            _uiCamera.fieldOfView = 60f;
            var canvas = _menu.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.planeDistance = 335.1518f;
        }

        protected void Update()
        {
            if (_menu == null || _menu.IsMouseUI)
                return;
            UpdateMovement();
            UpdateRotation();
        }

        private void UpdateMovement()
        {
            Vector3 direction = Vector3.zero;
            if (_input.SaveMap.GetKey())
                return;
            if (_input.Forward.GetKey())
                direction += Cache.Transform.forward;
            else if (_input.Back.GetKey())
                direction -= Cache.Transform.forward;
            if (_input.Right.GetKey())
                direction += Cache.Transform.right;
            else if (_input.Left.GetKey())
                direction -= Cache.Transform.right;
            if (_input.Up.GetKey())
                direction += Cache.Transform.up;
            else if (_input.Down.GetKey())
                direction -= Cache.Transform.up;
            float speed = _input.Slow.GetKey() ? _settings.CameraSlowMoveSpeed.Value : _settings.CameraMoveSpeed.Value;
             Cache.Transform.position += direction * Time.deltaTime * speed;
        }

        private void UpdateRotation()
        {
            float mouseX = Input.mousePosition.x;
            if (mouseX < _menu.GetMinMouseX() || mouseX > _menu.GetMaxMouseX())
                return;
            if (_input.RotateCamera.GetKey())
            {
                float inputX = Input.GetAxis("Mouse X");
                float inputY = Input.GetAxis("Mouse Y");
                float speed = _settings.CameraRotateSpeed.Value;
                Cache.Transform.RotateAround(Cache.Transform.position, Vector3.up, inputX * Time.deltaTime * speed);
                Cache.Transform.RotateAround(Cache.Transform.position, Cache.Transform.right, -inputY * Time.deltaTime * speed);
            }
        }

        protected override void SetDefaultCameraPosition()
        {
            _menu = (MapEditorMenu)UIManager.CurrentMenu;
            CreateUICamera();
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
    }
}
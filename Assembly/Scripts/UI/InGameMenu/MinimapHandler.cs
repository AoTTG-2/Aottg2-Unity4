using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Settings;
using Characters;
using GameManagers;
using ApplicationManagers;
using Utility;
using Cameras;

namespace UI
{
    class MinimapHandler : MonoBehaviour
    {
        public static Transform CameraTransform;
        private static Dictionary<string, Material> _cache = new Dictionary<string, Material>();
        private float _height;
        private static Color _mineColor = new Color(0.455f, 0.608f, 0.816f);
        private static Color _titanColor = new Color(1f, 1f, 0.44f);
        private static Color _humanColor = new Color(0.58f, 1f, 0.5f);
        private static Color _teamBlueColor = new Color(0f, 0.67f, 1f);
        private static Color _teamRedColor = new Color(0.87f, 0.25f, 0.25f);

        private void Awake()
        {
            var go = AssetBundleManager.InstantiateAsset<GameObject>("MinimapCamera", Vector3.zero, Quaternion.identity);
            CameraTransform = go.transform;
            _height = SettingsManager.GeneralSettings.MinimapHeight.Value;
            go.GetComponent<Camera>().orthographicSize = _height;
            go.GetComponent<Camera>().farClipPlane = _height + 1000f;
        }

        public void Disable()
        {
            CameraTransform.gameObject.SetActive(false);
        }

        public static void CreateMinimapIcon(BaseCharacter character)
        {
            if (!CameraTransform.gameObject.activeSelf)
                return;
            string texture;
            Color color = Color.white;
            string team = character.Team;
            if (team == TeamInfo.None)
                team = character is Human ? TeamInfo.Human : TeamInfo.Titan;
            if (character.IsMainCharacter())
            {
                team = "Mine";
                color = _mineColor;
            }
            if (character is Human)
                texture = "MinimapHumanIcon";
            else
                texture = "MinimapTitanIcon";
            if (team == TeamInfo.Human)
                color = _humanColor;
            else if (team == TeamInfo.Titan)
                color = _titanColor;
            else if (team == TeamInfo.Blue)
                color = _teamBlueColor;
            else if (team == TeamInfo.Red)
                color = _teamRedColor;
            var go = AssetBundleManager.InstantiateAsset<GameObject>("MinimapIcon", true);
            string hash = texture + team;
            if (!_cache.ContainsKey(hash))
            {
                go.renderer.material.SetTexture("_MainTex", (Texture2D)AssetBundleManager.LoadAsset(texture, true));
                go.renderer.material.SetColor("_Color", color);
                _cache.Add(hash, go.renderer.material);
            }
            else
                go.renderer.material = _cache[hash];
            var follow = go.AddComponent<MinimapIconFollow>();
            follow.Init(CameraTransform, character);
        }

        private void Update()
        {
            if (!CameraTransform.gameObject.activeSelf)
                return;
            var camera = (InGameCamera)SceneLoader.CurrentCamera;
            var position = camera.Cache.Transform.position;
            CameraTransform.position = new Vector3(position.x, position.y + _height, position.z);
            CameraTransform.rotation = Quaternion.Euler(new Vector3(90f, camera.Cache.Transform.rotation.eulerAngles.y, 0f));
        }
    }
}

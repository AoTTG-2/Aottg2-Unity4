using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Settings;
using Characters;
using GameManagers;
using ApplicationManagers;
using Utility;

namespace UI
{
    class MinimapIconFollow : MonoBehaviour
    {
        private BaseCharacter _character;
        private Transform _cameraTransform;
        private float scaleOffset;

        public void Init(Transform camera, BaseCharacter character)
        {
            _cameraTransform = camera;
            _character = character;
            var cam = _cameraTransform.GetComponent<Camera>();
            scaleOffset = cam.orthographicSize * 0.1f;
            transform.localScale = Vector3.one * cam.orthographicSize * 0.1f;
        }

        private void Update()
        {
            if (_character == null || _character.Dead)
            {
                Destroy(gameObject);
                return;
            }
            var position = _character.Cache.Transform.position;
            transform.position = new Vector3(position.x, _cameraTransform.position.y - scaleOffset * 0.5f - 10, position.z);
            transform.rotation = _cameraTransform.rotation;
            transform.RotateAround(transform.position, Vector3.up, 180f);
        }
    }
}

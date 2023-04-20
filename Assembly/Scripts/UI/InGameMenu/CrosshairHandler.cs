using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Settings;

namespace UI
{
    class CrosshairHandler : MonoBehaviour
    {
        public RawImage _crosshairImageWhite;
        public RawImage _crosshairImageRed;
        public Text _crosshairLabelWhite;
        public Text _crosshairLabelRed;
        public Image _arrowLeft;
        public Image _arrowRight;

        public void Awake()
        {
            _crosshairImageWhite = ElementFactory.InstantiateAndBind(transform, "CrosshairImage").GetComponent<RawImage>();
            _crosshairImageRed = ElementFactory.InstantiateAndBind(transform, "CrosshairImage").GetComponent<RawImage>();
            _crosshairImageRed.color = Color.red;
            _crosshairLabelWhite = _crosshairImageWhite.transform.Find("DefaultLabel").GetComponent<Text>();
            _crosshairLabelRed = _crosshairImageRed.transform.Find("DefaultLabel").GetComponent<Text>();
            _arrowLeft = ElementFactory.InstantiateAndBind(transform, "HookArrowImage").GetComponent<Image>();
            _arrowRight = ElementFactory.InstantiateAndBind(transform, "HookArrowImage").GetComponent<Image>();
            ElementFactory.SetAnchor(_crosshairImageWhite.gameObject, TextAnchor.MiddleCenter, TextAnchor.MiddleCenter, Vector2.zero);
            ElementFactory.SetAnchor(_crosshairImageRed.gameObject, TextAnchor.MiddleCenter, TextAnchor.MiddleCenter, Vector2.zero);
            _crosshairImageWhite.gameObject.AddComponent<CrosshairScaler>();
            _crosshairImageRed.gameObject.AddComponent<CrosshairScaler>();
            CursorManager.UpdateCrosshair(_crosshairImageWhite, _crosshairImageRed, _crosshairLabelWhite, _crosshairLabelRed, true);
        }

        private void Update()
        {
            CursorManager.UpdateCrosshair(_crosshairImageWhite, _crosshairImageRed, _crosshairLabelWhite, _crosshairLabelRed);
            CursorManager.UpdateHookArrows(_arrowLeft, _arrowRight);
        }
    }
}

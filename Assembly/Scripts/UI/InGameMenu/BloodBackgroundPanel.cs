﻿using ApplicationManagers;
using GameManagers;
using Settings;
using SimpleJSONFixed;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class BloodBackgroundPanel: BasePopup
    {
        protected override string Title => string.Empty;
        protected override float Width => 0f;
        protected override float Height => 0f;
        protected override float TopBarHeight => 0f;
        protected override float BottomBarHeight => 0f;
        protected RawImage _loadingBackground;
        protected override PopupAnimation PopupAnimationType => PopupAnimation.Fade;
        protected override float AnimationTime => 0.5f;

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            _loadingBackground = ElementFactory.InstantiateAndBind(transform, "MainBackground").transform.Find("Image").GetComponent<RawImage>();
        }

        public override void Show()
        {
            if (IsActive)
                return;
            string texture = "Blood" + Random.Range(1, 6).ToString() + "BackgroundTexture";
            _loadingBackground.texture = (Texture2D)AssetBundleManager.LoadAsset(texture, true);
            base.Show();
        }
    }
}

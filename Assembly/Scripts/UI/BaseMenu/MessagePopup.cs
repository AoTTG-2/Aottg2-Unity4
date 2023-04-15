﻿using Settings;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class MessagePopup: PromptPopup
    {
        protected override string Title => string.Empty;
        protected override float Width => 300f;
        protected override float Height => 240f;
        protected override int VerticalPadding => 30;
        protected override int HorizontalPadding => 30;
        protected override TextAnchor PanelAlignment => TextAnchor.MiddleCenter;

        protected float LabelHeight = 60f;
        private Text _label;

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            ElementStyle defaultStyle = new ElementStyle(themePanel: ThemePanel);
            ElementStyle buttonStyle = new ElementStyle(fontSize: ButtonFontSize, themePanel: ThemePanel);
            ElementFactory.CreateDefaultButton(BottomBar, buttonStyle, UIManager.GetLocaleCommon("Okay"), onClick: () => OnButtonClick("Okay"));
            _label = ElementFactory.CreateDefaultLabel(SinglePanel, defaultStyle, string.Empty).GetComponent<Text>();
            _label.GetComponent<LayoutElement>().preferredHeight = LabelHeight;
            _label.GetComponent<LayoutElement>().preferredWidth = GetWidth() - (HorizontalPadding * 2);
        }

        public void Show(string message)
        {
            if (gameObject.activeSelf)
                return;
            base.Show();
            _label.text = message;
        }

        private void OnButtonClick(string name)
        {
            Hide();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ApplicationManagers;
using Settings;
using SimpleJSONFixed;
using System.Collections;

namespace UI
{
    class InGameBackgroundMenu : BaseMenu
    {
        private BloodBackgroundPanel _bloodBackgroundPanel;
        private MainBackgroundPanel _mainBackgroundPanel;

        public override void Setup()
        {
            SetupMainBackground();
        }

        private void SetupMainBackground()
        {
            _mainBackgroundPanel = ElementFactory.CreateDefaultPopup<MainBackgroundPanel>(transform);
            _bloodBackgroundPanel = ElementFactory.CreateDefaultPopup<BloodBackgroundPanel>(transform);
            _mainBackgroundPanel.SetRandomBackground(loading: true);
            _mainBackgroundPanel.Show();
        }

        public void HideMainBackground()
        {
            _mainBackgroundPanel.Hide();
        }

        public void ShowBlood()
        {
            _bloodBackgroundPanel.Show();
        }

        public void HideBlood()
        {
            _bloodBackgroundPanel.Hide();
        }
    }
}

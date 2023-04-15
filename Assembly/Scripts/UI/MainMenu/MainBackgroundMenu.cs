﻿using System;
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
    class MainBackgroundMenu : BaseMenu
    {
        public MainBackgroundPanel _mainBackgroundPanelBack;
        public MainBackgroundPanel _mainBackgroundPanelFront;

        public override void Setup()
        {
            SetupMainBackground();
        }

        private void SetupMainBackground()
        {
            _mainBackgroundPanelBack = ElementFactory.CreateDefaultPopup<MainBackgroundPanel>(transform);
            _mainBackgroundPanelFront = ElementFactory.CreateDefaultPopup<MainBackgroundPanel>(transform);
            _mainBackgroundPanelBack.SetRandomBackground(loading: false);
            _mainBackgroundPanelBack.Show();
            _mainBackgroundPanelFront.BackgroundIndex = _mainBackgroundPanelBack.BackgroundIndex;
        }

        public void ChangeMainBackground()
        {
            _mainBackgroundPanelFront.SetRandomBackground(loading: false);
            _mainBackgroundPanelFront.Show();
            StartCoroutine(WaitAndFinishBackground());
        }

        private IEnumerator WaitAndFinishBackground()
        {
            yield return new WaitForSeconds(1.5f);
            _mainBackgroundPanelBack.SetBackground(loading: false, backgroundIndex: _mainBackgroundPanelFront.BackgroundIndex);
            _mainBackgroundPanelFront.HideImmediate();
        }
    }
}

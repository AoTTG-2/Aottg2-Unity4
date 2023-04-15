using ApplicationManagers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    class HeadedPanel: BasePanel
    {
        protected virtual string Title => "Default";
        protected virtual float TopBarHeight => 65f;
        protected virtual float BottomBarHeight => 65f;
        protected override float BorderVerticalPadding => 5f;
        protected override float BorderHorizontalPadding => 5f;
        protected override int VerticalPadding => 25;
        protected override int HorizontalPadding => 35;
        protected virtual int TitleFontSize => 30;
        protected virtual int ButtonFontSize => 28;
        protected virtual bool CategoryButtons => false;
        protected Transform BottomBar;
        protected Transform TopBar;
        protected Dictionary<string, Button> _topButtons = new Dictionary<string, Button>();

        public override void Setup(BasePanel parent = null)
        {
            TopBar = transform.Find("Background/TopBar");
            BottomBar = transform.Find("Background/BottomBar");
            Transform topBarLine = transform.Find("Background/TopBarLine");
            Transform bottomBarLine = transform.Find("Background/BottomBarLine");
            TopBar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TopBarHeight);
            BottomBar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BottomBarHeight);
            topBarLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -TopBarHeight);
            bottomBarLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, BottomBarHeight);
            if (TopBar.Find("Label") != null)
            {
                if (CategoryButtons)
                {
                    TopBar.Find("Label").gameObject.SetActive(false);
                }
                else
                {
                    TopBar.Find("Label").GetComponent<Text>().fontSize = TitleFontSize;
                    TopBar.Find("Label").GetComponent<Text>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "TitleColor");
                    SetTitle(Title);
                }
            }
            TopBar.GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "TopBarColor");
            BottomBar.GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BottomBarColor");
            topBarLine.GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BorderColor");
            bottomBarLine.GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BorderColor");
            transform.Find("Border").GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BorderColor");
            transform.Find("Background").GetComponent<Image>().color = UIManager.GetThemeColor(ThemePanel, "MainBody", "BackgroundColor");
            if (CategoryButtons)
            {
                SetupTopButtons();
            }
            base.Setup(parent);
        }

        public override void SetCategoryPanel(string name)
        {
            base.SetCategoryPanel(name);
            SetTopButton(name);
        }

        protected virtual void SetTopButton(string name)
        {
            if (_topButtons.Count > 0)
            {
                foreach (Button button in _topButtons.Values)
                    button.interactable = true;
                _topButtons[name].interactable = false;
            }
        }

        protected void SetTitle(string title)
        {
            TopBar.Find("Label").GetComponent<Text>().text = title;
        }

        protected virtual void SetupTopButtons()
        {
            Canvas.ForceUpdateCanvases();
            float totalButtonWidth = 0f;
            foreach (Button button in _topButtons.Values)
                totalButtonWidth += button.GetComponent<RectTransform>().rect.width;
            TopBar.GetComponent<HorizontalLayoutGroup>().spacing = (GetWidth() - totalButtonWidth) / (_topButtons.Count + 1);
        }

        public override float GetPanelHeight()
        {
            float topBarHeight = Mathf.Max(TopBar.GetComponent<RectTransform>().sizeDelta.y, BorderVerticalPadding);
            float bottomBarHeight = Mathf.Max(BottomBar.GetComponent<RectTransform>().sizeDelta.y, BorderVerticalPadding);
            return GetHeight() - topBarHeight - bottomBarHeight - BorderVerticalPadding * 2f;
        }
    }
}

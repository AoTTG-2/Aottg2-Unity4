using Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    class TutorialPopup: MainMenuPopup
    {
        protected override string Title => UIManager.GetLocale("MainMenu", "TutorialPopup", "Title");
        protected override float Width => 280f;
        protected override float Height => 300f;
        protected override float VerticalSpacing => 20f;
        protected override int VerticalPadding => 20;
        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            string cat = "MainMenu";
            string sub = "TutorialPopup";
            float elementWidth = 210f;
            ElementStyle style = new ElementStyle(fontSize: ButtonFontSize, themePanel: ThemePanel);
            ElementFactory.CreateDefaultButton(BottomBar, style, UIManager.GetLocaleCommon("Back"), onClick: () => OnButtonClick("Back"));
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "BasicButton"), onClick: () => OnButtonClick("Basic"), 
                elementWidth: elementWidth);
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "AdvancedButton"), onClick: () => OnButtonClick("Advanced"),
                elementWidth: elementWidth);
        }

        protected void OnButtonClick(string name)
        {
            if (name == "MapEditor")
            {
                Application.LoadLevel(2);
            }
            else if (name == "CharacterEditor")
            {
                Application.LoadLevel("characterCreation");
            }
            else if (name == "SnapshotViewer")
            {
                Application.LoadLevel("SnapShot");
            }
            else if (name == "Back")
                Hide();
        }
    }
}

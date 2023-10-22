using ApplicationManagers;
using Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    class ToolsPopup: MainMenuPopup
    {
        protected override string Title => UIManager.GetLocale("MainMenu", "ToolsPopup", "Title");
        protected override float Width => 280f;
        protected override float Height => 420f;
        protected override float VerticalSpacing => 20f;
        protected override int VerticalPadding => 20;
        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            string cat = "MainMenu";
            string sub = "ToolsPopup";
            float elementWidth = 210f;
            ElementStyle style = new ElementStyle(fontSize: ButtonFontSize, themePanel: ThemePanel);
            ElementFactory.CreateDefaultButton(BottomBar, style, UIManager.GetLocaleCommon("Back"), onClick: () => OnButtonClick("Back"));
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "MapEditorButton"), onClick: () => OnButtonClick("MapEditor"), 
                elementWidth: elementWidth);
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "CharacterEditorButton"), onClick: () => OnButtonClick("CharacterEditor"),
                elementWidth: elementWidth);
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "SnapshotViewerButton"), onClick: () => OnButtonClick("SnapshotViewer"),
                elementWidth: elementWidth);
            ElementFactory.CreateDefaultButton(SinglePanel, style, UIManager.GetLocale(cat, sub, "GalleryButton"), onClick: () => OnButtonClick("Gallery"),
                elementWidth: elementWidth);
        }

        protected void OnButtonClick(string name)
        {
            if (name == "MapEditor")
                SceneLoader.LoadScene(SceneName.MapEditor);
            else if (name == "CharacterEditor")
                SceneLoader.LoadScene(SceneName.CharacterEditor);
            else if (name == "SnapshotViewer")
                SceneLoader.LoadScene(SceneName.SnapshotViewer);
            else if (name == "Gallery")
                SceneLoader.LoadScene(SceneName.Gallery);
            else if (name == "Back")
                Hide();
        }
    }
}

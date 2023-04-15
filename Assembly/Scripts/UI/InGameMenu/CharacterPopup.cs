using System;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using Settings;
using ApplicationManagers;
using GameManagers;
using CustomLogic;

namespace UI
{
    class CharacterPopup : BasePopup
    {
        protected override string Title => string.Empty;
        protected override float Width => 1010f;
        protected override float Height => 400f;
        protected override bool CategoryPanel => true;
        protected override bool CategoryButtons => true;
        protected override string DefaultCategoryPanel => "Human";
        public string LocaleCategory = "CharacterPopup";

        public override void Setup(BasePanel parent = null)
        {
            base.Setup(parent);
            SetupBottomButtons();
        }

        protected override void SetupTopButtons()
        {
            ElementStyle style = new ElementStyle(fontSize: 28, themePanel: ThemePanel);
            List<string> categories = new List<string>();
            InGameMiscSettings settings = SettingsManager.InGameCurrent.Misc;
            if (settings.AllowGuns.Value || settings.AllowBlades.Value || settings.AllowThunderspears.Value)
                categories.Add("Human");
            if (settings.AllowPlayerTitans.Value)
                categories.Add("Titan");
            if (settings.AllowShifters.Value)
                categories.Add("Shifter");
            foreach (string buttonName in categories)
            {
                GameObject obj = ElementFactory.CreateCategoryButton(TopBar, style, UIManager.GetLocaleCommon(buttonName),
                    onClick: () => SetCategoryPanel(buttonName));
                _topButtons.Add(buttonName, obj.GetComponent<Button>());
            }
            base.SetupTopButtons();
        }

        protected override void RegisterCategoryPanels()
        {
            _categoryPanelTypes.Add("Human", typeof(CharacterHumanPanel));
            _categoryPanelTypes.Add("Titan", typeof(CharacterTitanPanel));
            _categoryPanelTypes.Add("Shifter", typeof(CharacterShifterPanel));
        }

        private void SetupBottomButtons()
        {
            ElementStyle style = new ElementStyle(fontSize: ButtonFontSize, themePanel: ThemePanel);
            ElementFactory.CreateDefaultButton(BottomBar, style, UIManager.GetLocale(LocaleCategory, "Bottom", "SpectateButton"),
                    onClick: () => OnBottomBarButtonClick("Spectate"));
            ElementFactory.CreateDefaultButton(BottomBar, style, UIManager.GetLocaleCommon("Join"),
                    onClick: () => OnBottomBarButtonClick("Join"));
        }

        private void OnBottomBarButtonClick(string name)
        {
            var manager = (InGameManager)SceneLoader.CurrentGameManager;
            switch (name)
            {
                case "Spectate":
                    SettingsManager.InGameCharacterSettings.ChooseStatus.Value = (int)ChooseCharacterStatus.Spectating;
                    InGameManager.UpdatePlayerName();
                    InGameManager.UpdateRoundPlayerProperties();
                    Hide();
                    break;
                case "Join":
                    SettingsManager.InGameCharacterSettings.ChooseStatus.Value = (int)ChooseCharacterStatus.Chosen;
                    bool canJoin = PhotonNetwork.isMasterClient || CustomLogicManager.Evaluator.CurrentTime < SettingsManager.InGameCurrent.Misc.AllowSpawnTime.Value;
                    if (canJoin && !manager.HasSpawned)
                        manager.SpawnPlayer(false);
                    InGameManager.UpdateRoundPlayerProperties();
                    Hide();
                    break;
            }
        }
    }
}

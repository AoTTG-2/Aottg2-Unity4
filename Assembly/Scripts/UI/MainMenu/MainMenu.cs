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
    class MainMenu: BaseMenu
    {
        public BasePopup _createGamePopup;
        public BasePopup _multiplayerMapPopup;
        public BasePopup _settingsPopup;
        public BasePopup _toolsPopup;
        public BasePopup _multiplayerRoomListPopup;
        public BasePopup _editProfilePopup;
        public BasePopup _leaderboardPopup;
        public BasePopup _socialPopup;
        public BasePopup _aboutPopup;
        public BasePopup _questPopup;
        public BasePopup _tutorialPopup;
        public MainBackgroundMenu _backgroundMenu;
        public TipPanel _tipPanel;
        protected Dictionary<string, AudioSource> _sounds = new Dictionary<string, AudioSource>();
        protected Text _multiplayerStatusLabel;
        protected string _lastButtonClicked;
        public static JSONNode MainBackgroundInfo = null;
        protected const float ChangeBackgroundTime = 20f;

        public override void Setup()
        {
            base.Setup();
            if (MainBackgroundInfo == null)
                MainBackgroundInfo = JSON.Parse(AssetBundleManager.LoadText("MainBackgroundInfo"));
            var go = AssetBundleManager.InstantiateAsset<GameObject>("MainMenuSounds");
            foreach (var source in go.GetComponentsInChildren<AudioSource>())
                _sounds.Add(source.name, source);
            SetupMainBackground();
            SetupIntroPanel();
            SetupLabels();
            
        }

        public void PlaySound(string sound)
        {
            _sounds[sound].Play();
        }

        private void SetupMainBackground()
        {
            _backgroundMenu = ElementFactory.CreateMenu<MainBackgroundMenu>("BackgroundMenu");
            _backgroundMenu.Setup();
            _tipPanel = ElementFactory.CreateTipPanel(transform, enabled: true);
            _tipPanel.SetRandomTip();
            ElementFactory.SetAnchor(_tipPanel.gameObject, TextAnchor.LowerRight, TextAnchor.LowerRight, new Vector2(10f, -10f));
            StartCoroutine(WaitAndChangeBackground());
        }

        public void ShowMultiplayerRoomListPopup()
        {
            HideAllPopups();
            _multiplayerRoomListPopup.Show();
        }

        public void ShowMultiplayerMapPopup()
        {
            HideAllPopups();
            _multiplayerMapPopup.Show();
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();
            _createGamePopup = ElementFactory.CreateHeadedPanel<CreateGamePopup>(transform).GetComponent<CreateGamePopup>();
            _multiplayerMapPopup = ElementFactory.InstantiateAndSetupPanel<MultiplayerMapPopup>(transform, "MultiplayerMapPopup").
                GetComponent<BasePopup>();
            _editProfilePopup = ElementFactory.CreateHeadedPanel<EditProfilePopup>(transform).GetComponent<BasePopup>();
            _settingsPopup = ElementFactory.CreateHeadedPanel<SettingsPopup>(transform).GetComponent<BasePopup>();
            _toolsPopup = ElementFactory.CreateHeadedPanel<ToolsPopup>(transform).GetComponent<BasePopup>();
            _multiplayerRoomListPopup = ElementFactory.InstantiateAndSetupPanel<MultiplayerRoomListPopup>(transform, "MultiplayerRoomListPopup").
                GetComponent<BasePopup>();
            _leaderboardPopup = ElementFactory.CreateHeadedPanel<LeaderboardPopup>(transform).GetComponent<BasePopup>();
            _socialPopup = ElementFactory.CreateHeadedPanel<SocialPopup>(transform).GetComponent<BasePopup>();
            _aboutPopup = ElementFactory.CreateHeadedPanel<AboutPopup>(transform).GetComponent<BasePopup>();
            _questPopup = ElementFactory.CreateHeadedPanel<QuestPopup>(transform).GetComponent<BasePopup>();
            _tutorialPopup = ElementFactory.CreateHeadedPanel<TutorialPopup>(transform).GetComponent<BasePopup>();
            _popups.Add(_createGamePopup);
            _popups.Add(_multiplayerMapPopup);
            _popups.Add(_editProfilePopup);
            _popups.Add(_settingsPopup);
            _popups.Add(_toolsPopup);
            _popups.Add(_multiplayerRoomListPopup);
            _popups.Add(_leaderboardPopup);
            _popups.Add(_socialPopup);
            _popups.Add(_aboutPopup);
            _popups.Add(_questPopup);
            _popups.Add(_tutorialPopup);
        }

        private void SetupIntroPanel()
        {
            GameObject introPanel = ElementFactory.InstantiateAndBind(transform, "IntroPanel");
            introPanel.AddComponent<IgnoreScaler>();
            ElementFactory.SetAnchor(introPanel, TextAnchor.UpperLeft, TextAnchor.UpperLeft, new Vector2(0f, 0f));
            foreach (Transform transform in introPanel.transform.Find("Buttons"))
            {
                IntroButton introButton = transform.gameObject.AddComponent<IntroButton>();
                introButton.onClick.AddListener(() => OnIntroButtonClick(introButton.name));
            }
            foreach (Transform transform in introPanel.transform.Find("Icons"))
            {
                Button button = transform.gameObject.GetComponent<Button>();
                button.onClick.AddListener(() => OnIntroButtonClick(transform.name));
                ColorBlock block = new ColorBlock
                {
                    colorMultiplier = 1f,
                    fadeDuration = 0.1f,
                    normalColor = new Color(0.9f, 0.9f, 0.9f),
                    highlightedColor = new Color(0.75f, 0.75f, 0.75f),
                    pressedColor = new Color(0.5f, 0.5f, 0.5f),
                    disabledColor = new Color(0.5f, 0.5f, 0.5f)
                };
                button.colors = block;
            }
        }

        private void SetupLabels()
        {
            _multiplayerStatusLabel = ElementFactory.CreateDefaultLabel(transform, ElementStyle.Default, string.Empty, alignment: TextAnchor.MiddleLeft).GetComponent<Text>();
            ElementFactory.SetAnchor(_multiplayerStatusLabel.gameObject, TextAnchor.UpperLeft, TextAnchor.UpperLeft, new Vector2(20f, -20f));
            _multiplayerStatusLabel.color = Color.black;
            Text versionText = ElementFactory.CreateDefaultLabel(transform, ElementStyle.Default, string.Empty).GetComponent<Text>();
            ElementFactory.SetAnchor(versionText.gameObject, TextAnchor.LowerCenter, TextAnchor.LowerCenter, new Vector2(0f, 20f));
            versionText.color = Color.white;
            if (ApplicationConfig.DevelopmentMode)
                versionText.text = "AOTTG2 DEVELOPMENT VERSION";
            else
                versionText.text = "AOTTG2 Version " + ApplicationConfig.GameVersion + ".";
            versionText.text = "";
        }

        private void ChangeMainBackground()
        {
            _backgroundMenu.ChangeMainBackground();
            _tipPanel.SetRandomTip();
        }

        private IEnumerator WaitAndChangeBackground()
        {
            while (true)
            {
                yield return new WaitForSeconds(ChangeBackgroundTime);
                ChangeMainBackground();
            }
        }

        private void Update()
        {
            if (_multiplayerStatusLabel != null)
            {
                string label = "";
                if (SettingsManager.GraphicsSettings.ShowFPS.Value)
                    label = "FPS:" + UIManager.GetFPS().ToString() + "\n";
                if (_multiplayerMapPopup.IsActive || _multiplayerRoomListPopup.IsActive)
                {
                    label += PhotonNetwork.connectionStateDetailed.ToString();
                    if (PhotonNetwork.connected)
                        label += " Ping:" + PhotonNetwork.GetPing().ToString();
                }
                _multiplayerStatusLabel.text = label;
            }
        }

        private bool IsPopupActive()
        {
            foreach (BasePopup popup in _popups)
            {
                if (popup.IsActive)
                    return true;
            }
            return false;
        }

        private void OnIntroButtonClick(string name)
        {
            bool isPopupAactive = IsPopupActive();
            HideAllPopups();
            if (isPopupAactive && _lastButtonClicked == name)
                return;
            PlaySound("Forward");
            _lastButtonClicked = name;
            switch (name)
            {
                case "TutorialButton":
                    _tutorialPopup.Show();
                    break;
                case "SingleplayerButton":
                    ((CreateGamePopup)_createGamePopup).Show(false);
                    break;
                case "MultiplayerButton":
                    _multiplayerMapPopup.Show();
                    break;
                case "ProfileButton":
                    _editProfilePopup.Show();
                    break;
                case "SettingsButton":
                    _settingsPopup.Show();
                    break;
                case "ToolsButton":
                    _toolsPopup.Show();
                    break;
                case "QuitButton":
                    Application.Quit();
                    break;
                case "QuestButton":
                    _questPopup.Show();
                    break;
                case "LeaderboardButton":
                    _leaderboardPopup.Show();
                    break;
                case "SocialButton":
                    _socialPopup.Show();
                    break;
                case "HelpButton":
                    _aboutPopup.Show();
                    break;
                case "PatreonButton":
                    ExternalLinkPopup.Show("https://www.patreon.com/aottg2");
                    break;
            }
        }
    }
}

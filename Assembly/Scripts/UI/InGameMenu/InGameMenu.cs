using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Settings;
using GameManagers;
using Utility;
using SimpleJSONFixed;
using ApplicationManagers;
using Characters;
using System.Collections;
using Cameras;

namespace UI
{
    class InGameMenu: BaseMenu
    {
        public EmoteHandler EmoteHandler;
        public ItemHandler ItemHandler;
        public CharacterInfoHandler CharacterInfoHandler;
        public ChatPanel ChatPanel;
        public BasePopup _settingsPopup;
        public BasePopup _createGamePopup;
        public BasePopup _pausePopup;
        public BasePopup _characterPopup;
        public BasePopup _scoreboardPopup;
        public CutsceneDialoguePanel _cutsceneDialoguePanel;
        private TipPanel _tipPanel;
        private LoadingProgressPanel _loadingProgressPanel;
        private InGameBackgroundMenu _backgroundMenu;
        private KillFeedPopup _killFeedPopup;
        private KillScorePopup _killScorePopup;
        private Text _topCenterLabel;
        private Text _topLeftLabel;
        private Text _topRightLabel;
        private Text _middleCenterLabel;
        private Text _bottomLeftLabel;
        private Text _bottomRightLabel;
        private Text _bottomCenterLabel;
        private GameObject _hudBottom;
        private Image _hudBottomSpecialFillBackground;
        private Image _hudBottomSpecialFillIcon;
        private Image _hudBottomGasFill;
        private Image _hudBottomBladeFill;
        private Text _hudBottomLeftLabel;
        private Text _hudBottomRightLabel;
        private Text _hudBottomGasLabel;
        private string _currentSpecialIcon = "";
        private string _specialIcon = "";
        private Human _human;
        private bool _showingBlood;
        private List<BasePopup> _allPausePopups = new List<BasePopup>();
        private Dictionary<string, float> _labelTimeLeft = new Dictionary<string, float>();
        private Dictionary<string, bool> _labelHasTimeLeft = new Dictionary<string, bool>();
        private float _killFeedTimeLeft;
        private float _killScoreTimeLeft;
        private string _middleCenterText;
        private string _bottomRightText;
        private string _bottomCenterText;
        private string _topLeftText;
        private InGameManager _gameManager;
        private Color _goldColor = new Color(1f, 0.73f, 0f);
        private Color _greenColor = new Color(0f, 0.75f, 0f);
        
        public override void Setup()
        {
            base.Setup();
            SetupLoading();
            SetupLabels();
            EmoteHandler = gameObject.AddComponent<EmoteHandler>();
            ItemHandler = gameObject.AddComponent<ItemHandler>();
            CharacterInfoHandler = gameObject.AddComponent<CharacterInfoHandler>();
            gameObject.AddComponent<CrosshairHandler>();
            SetupChat();
            SetupMinimap();
            HideAllMenus();
        }

        public void SetSpecialIcon(string icon)
        {
            _specialIcon = icon;
        }

        public void SetupMinimap()
        {
            gameObject.AddComponent<MinimapHandler>();
            if (SettingsManager.GeneralSettings.MinimapEnabled.Value && !SettingsManager.InGameCurrent.Misc.GlobalMinimapDisable.Value)
            {
                var minimap = ElementFactory.InstantiateAndBind(transform, "MinimapPanel");
                ElementFactory.SetAnchor(minimap, TextAnchor.UpperRight, TextAnchor.UpperRight, new Vector2(-10f, -10f));
            }
            else
                GetComponent<MinimapHandler>().Disable();
        }

        public void UpdateLoading(float percentage, bool finished = false)
        {
            percentage = Mathf.Clamp(percentage, 0f, 1f);
            _loadingProgressPanel.Show(percentage);
            if (finished)
                OnFinishLoading();
        }

        private void SetupChat()
        {
            ChatPanel = ElementFactory.InstantiateAndSetupPanel<ChatPanel>(transform, "ChatPanel", true).GetComponent<ChatPanel>();
            ElementFactory.SetAnchor(ChatPanel.gameObject, TextAnchor.LowerLeft, TextAnchor.LowerLeft, new Vector2(10f, 10f));
        }

        private void SetupLabels()
        {
            ElementStyle style = new ElementStyle(fontSize: 22);
            _topCenterLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleCenter).GetComponent<Text>();
            ElementFactory.SetAnchor(_topCenterLabel.gameObject, TextAnchor.UpperCenter, TextAnchor.UpperCenter, new Vector2(0f, -10f));
            _topLeftLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleLeft).GetComponent<Text>();
            ElementFactory.SetAnchor(_topLeftLabel.gameObject, TextAnchor.UpperLeft, TextAnchor.UpperLeft, new Vector2(10f, -10f));
            _topRightLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleRight).GetComponent<Text>();
            ElementFactory.SetAnchor(_topRightLabel.gameObject, TextAnchor.UpperRight, TextAnchor.UpperRight, new Vector2(-10f, -10f));
            _middleCenterLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleCenter).GetComponent<Text>();
            ElementFactory.SetAnchor(_middleCenterLabel.gameObject, TextAnchor.MiddleCenter, TextAnchor.MiddleCenter, new Vector2(0f, 100f));
            _bottomCenterLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleCenter).GetComponent<Text>();
            ElementFactory.SetAnchor(_bottomCenterLabel.gameObject, TextAnchor.LowerCenter, TextAnchor.LowerCenter, new Vector2(0f, 10f));
            _bottomLeftLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleLeft).GetComponent<Text>();
            ElementFactory.SetAnchor(_bottomLeftLabel.gameObject, TextAnchor.LowerLeft, TextAnchor.LowerLeft, new Vector2(10f, 10f));
            _bottomRightLabel = ElementFactory.CreateHUDLabel(transform, style, "", FontStyle.Normal, TextAnchor.MiddleRight).GetComponent<Text>();
            ElementFactory.SetAnchor(_bottomRightLabel.gameObject, TextAnchor.LowerRight, TextAnchor.LowerRight, new Vector2(-10f, 10f));
            _killFeedPopup = ElementFactory.CreateDefaultPopup<KillFeedPopup>(transform);
            ElementFactory.SetAnchor(_killFeedPopup.gameObject, TextAnchor.UpperCenter, TextAnchor.MiddleCenter, new Vector2(0f, -120f));
            _killScorePopup = ElementFactory.CreateDefaultPopup<KillScorePopup>(transform);
            ElementFactory.SetAnchor(_killScorePopup.gameObject, TextAnchor.MiddleCenter, TextAnchor.MiddleCenter, new Vector2(0f, 100f));
        }

        private void SetupLoading()
        {
            _backgroundMenu = ElementFactory.CreateMenu<InGameBackgroundMenu>("BackgroundMenu");
            _backgroundMenu.Setup();
            _backgroundMenu.transform.SetAsFirstSibling();
            _loadingProgressPanel = ElementFactory.CreateDefaultPopup<LoadingProgressPanel>(transform);
            _tipPanel = ElementFactory.CreateTipPanel(transform, enabled: true);
            _tipPanel.SetRandomTip();
            ElementFactory.SetAnchor(_tipPanel.gameObject, TextAnchor.LowerRight, TextAnchor.LowerRight, new Vector2(10f, -10f));
            _loadingProgressPanel.Show(0f);
            UpdateLoading(0f);
        }

        private void OnFinishLoading()
        {
            _tipPanel.gameObject.SetActive(false);
            _loadingProgressPanel.Hide();
            _backgroundMenu.HideMainBackground();
            _characterPopup = ElementFactory.CreateDefaultPopup<CharacterPopup>(transform);
            _scoreboardPopup = ElementFactory.CreateDefaultPopup<ScoreboardPopup>(transform);
            _cutsceneDialoguePanel = ElementFactory.CreateDefaultPopup<CutsceneDialoguePanel>(transform);
            ElementFactory.SetAnchor(_cutsceneDialoguePanel.gameObject, TextAnchor.LowerCenter, TextAnchor.LowerCenter, new Vector2(0f, 100f));
            _popups.Add(_characterPopup);
            _popups.Add(_scoreboardPopup);
            _gameManager = (InGameManager)SceneLoader.CurrentGameManager;
        }

        public static bool InMenu()
        {
            var menu = (InGameMenu)UIManager.CurrentMenu;
            foreach (BasePopup popup in menu._popups)
            {
                if (popup.IsActive)
                    return true;
            }
            return menu.EmoteHandler.IsActive || menu.ItemHandler.IsActive;
        }

        public void SetPauseMenu(bool enabled)
        {
            if (enabled && !IsPauseMenuActive())
            {
                HideAllMenus();
                _pausePopup.Show();
            }            else if (!enabled)
            {
                HideAllMenus();
            }
        }

        public void ToggleScoreboardMenu()
        {
            SetScoreboardMenu(!_scoreboardPopup.gameObject.activeSelf);
        }

        public void SetScoreboardMenu(bool enabled)
        {
            if (enabled && !InMenu())
            {
                HideAllMenus();
                _scoreboardPopup.Show();
            }
            else if (!enabled)
            {
                _scoreboardPopup.Hide();
            }
        }

        public void SetCharacterMenu(bool enabled)
        {
            if (enabled && !InMenu())
            {
                HideAllMenus();
                _characterPopup.Show();
                InGameManager.UpdateRoundPlayerProperties();
            }
            else if (!enabled)
                _characterPopup.Hide();
        }

        public void ShowCutsceneMenu(string icon, string title, string content)
        {
            _cutsceneDialoguePanel.Show(icon, title, content);
        }

        public void HideCutsceneMenu()
        {
            _cutsceneDialoguePanel.Hide();
        }

        public bool IsPauseMenuActive()
        {
            foreach (BasePopup popup in _allPausePopups)
            {
                if (popup.gameObject.activeSelf)
                    return true;
            }
            return false;
        }

        public void ShowBlood()
        {
            if (!_showingBlood)
            {
                _showingBlood = true;
                StartCoroutine(WaitAndShowBlood());
            }
        }

        public void ShowKillFeed(string killer, string victim, int score)
        {
            _killFeedPopup.Show(killer, victim, score);
            _killFeedTimeLeft = 5f;
        }

        public void ShowKillScore(int score)
        {
            _killScorePopup.Show(score);
            _killScoreTimeLeft = 3f;
        }

        public void SetLabel(string label, string message, float time)
        {
            SetLabelText(label, message);
            if (time == 0f)
                _labelHasTimeLeft[label] = false;
            else
                _labelHasTimeLeft[label] = true;
            _labelTimeLeft[label] = time;
        }

        private void SetLabelText(string label, string message)
        {
            if (label == "TopCenter")
                _topCenterLabel.text = message;
            else if (label == "TopLeft")
                _topLeftText = message;
            else if (label == "TopRight")
                _topRightLabel.text = message;
            else if (label == "MiddleCenter")
                _middleCenterText = message;
            else if (label == "BottomLeft")
                _bottomLeftLabel.text = message;
            else if (label == "BottomRight")
                _bottomRightText = message;
            else if (label == "BottomCenter")
                _bottomCenterText = message;
        }

        IEnumerator WaitAndShowBlood()
        {
            _backgroundMenu.ShowBlood();
            yield return new WaitForSeconds(5f);
            _backgroundMenu.HideBlood();
            _showingBlood = false;
        }

        public void SetBottomHUD(Human myHuman = null)
        {
            _human = myHuman;
            if (_hudBottom != null)
                Destroy(_hudBottom);
            if (_human == null)
                return;
            if (_human.Setup.Weapon == HumanWeapon.Gun)
                _hudBottom = ElementFactory.InstantiateAndBind(transform, "HUDBottomGun");
            else if (_human.Setup.Weapon == HumanWeapon.Thunderspear)
                _hudBottom = ElementFactory.InstantiateAndBind(transform, "HUDBottomTS");
            else
            {
                _hudBottom = ElementFactory.InstantiateAndBind(transform, "HUDBottomBlade");
                _hudBottomBladeFill = _hudBottom.transform.Find("BladeFill").GetComponent<Image>();
            }
            ElementFactory.SetAnchor(_hudBottom, TextAnchor.LowerCenter, TextAnchor.LowerCenter, Vector3.up * 10f);
            _hudBottomSpecialFillBackground = _hudBottom.transform.Find("SkillFillBackground").GetComponent<Image>();
            _hudBottomSpecialFillIcon = _hudBottom.transform.Find("SkillFillIcon").GetComponent<Image>();
            _hudBottomGasFill = _hudBottom.transform.Find("GasFill").GetComponent<Image>();
            _hudBottomLeftLabel = _hudBottom.transform.Find("LeftLabel").GetComponent<Text>();
            _hudBottomRightLabel = _hudBottom.transform.Find("RightLabel").GetComponent<Text>();
            _hudBottomGasLabel = _hudBottom.transform.Find("GasLabel").GetComponent<Text>();
            _currentSpecialIcon = "";
        }

        void Update()
        {
            if (_gameManager == null)
                return;
            if (_human != null)
            {
                UpdateHumanSpecial();
                UpdateHumanHUD();
            }
            foreach (string label in new List<string>(_labelHasTimeLeft.Keys))
            {
                if (_labelHasTimeLeft[label])
                {
                    _labelTimeLeft[label] -= Time.deltaTime;
                    if (_labelTimeLeft[label] <= 0f)
                    {
                        _labelHasTimeLeft[label] = false;
                        SetLabelText(label, "");
                    }
                }
            }
            if (_gameManager.IsEnding)
                _middleCenterLabel.text = _middleCenterText + "\n" + "Restarting in " + ((int)_gameManager.EndTimeLeft).ToString();
            else
                _middleCenterLabel.text = _middleCenterText;
            var inGame = SettingsManager.InGameCharacterSettings;
            if (inGame.ChooseStatus.Value == (int)ChooseCharacterStatus.Spectating || (_gameManager.CurrentCharacter == null || _gameManager.CurrentCharacter.Dead))
            {
                var input = SettingsManager.InputSettings.General;
                string spectating = "";
                if (inGame.ChooseStatus.Value != (int)ChooseCharacterStatus.Choosing)
                {
                    spectating = "Prev: " + ChatManager.GetColorString(input.SpectatePreviousPlayer.ToString(), ChatTextColor.System) + ", ";
                    spectating += "Next: " + ChatManager.GetColorString(input.SpectateNextPlayer.ToString(), ChatTextColor.System) + ", ";
                    spectating += "Join: " + ChatManager.GetColorString(input.ChangeCharacter.ToString(), ChatTextColor.System);
                }
                var camera = (InGameCamera)SceneLoader.CurrentCamera;
                if (camera._follow != null && camera._follow != _gameManager.CurrentCharacter)
                    spectating = "Spectating " + camera._follow.Name + ". " + spectating;
                else
                    spectating = "Spectating. " + spectating;
                _bottomCenterLabel.text = _bottomCenterText + "\n" + spectating;
            }
            else
                _bottomCenterLabel.text = _bottomCenterText;
            _bottomRightLabel.text = _bottomRightText + GetKeybindStrings();
            string telemetrics = "";
            if (SettingsManager.GraphicsSettings.ShowFPS.Value)
                telemetrics = "FPS:" + UIManager.GetFPS().ToString() + "\n";
            if (!PhotonNetwork.offlineMode && SettingsManager.UISettings.ShowPing.Value)
                telemetrics += "Ping:" + PhotonNetwork.GetPing().ToString() + "\n";
            _topLeftLabel.text = telemetrics + _topLeftText;
            _killFeedTimeLeft -= Time.deltaTime;
            if (_killFeedPopup.IsActive && _killFeedTimeLeft <= 0f)
                _killFeedPopup.Hide();
            _killScoreTimeLeft -= Time.deltaTime;
            if (_killScorePopup.IsActive && _killScoreTimeLeft <= 0f)
                _killScorePopup.Hide();
        }

        private void UpdateHumanSpecial()
        {
            if (_human.Special == null)
            {
                _hudBottomSpecialFillBackground.fillAmount = 0f;
                if (_hudBottomSpecialFillIcon.gameObject.activeSelf)
                    _hudBottomSpecialFillIcon.gameObject.SetActive(false);
            }
            else
            {
                var ratio = _human.Special.GetCooldownRatio();
                _hudBottomSpecialFillBackground.fillAmount = ratio;
                if (_currentSpecialIcon != _specialIcon)
                {
                    _currentSpecialIcon = _specialIcon;
                    if (_currentSpecialIcon != "")
                    {
                        var icon = (Texture2D)AssetBundleManager.LoadAsset(_currentSpecialIcon, true);
                        var sprite = UnityEngine.Sprite.Create(icon, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f));
                        _hudBottomSpecialFillIcon.sprite = sprite;
                    }
                }
                if (_currentSpecialIcon == "")
                {
                    if (_hudBottomSpecialFillIcon.gameObject.activeSelf)
                        _hudBottomSpecialFillIcon.gameObject.SetActive(false);
                }
                else
                {
                    if (!_hudBottomSpecialFillIcon.gameObject.activeSelf)
                        _hudBottomSpecialFillIcon.gameObject.SetActive(true);
                    _hudBottomSpecialFillIcon.fillAmount = ratio;
                }
            }
        }

        private void UpdateHumanHUD()
        {
            float gasRatio;
            if (_human.MaxGas <= 0f)
                gasRatio = 0f;
            else
                gasRatio = _human.CurrentGas / _human.MaxGas;
            _hudBottomGasFill.fillAmount = gasRatio;
            _hudBottomGasLabel.text = ((int)(gasRatio * 100f)).ToString() + "%";
            if (gasRatio <= 0.2f)
                _hudBottomGasLabel.color = Color.red;
            else
                _hudBottomGasLabel.color = _goldColor;
            if (_human.Weapon is BladeWeapon)
            {
                var weapon = (BladeWeapon)_human.Weapon;
                _hudBottomBladeFill.fillAmount = weapon.CurrentDurability / weapon.MaxDurability;
                float percent = (int)(weapon.CurrentDurability * 100f / weapon.MaxDurability);
                _hudBottomLeftLabel.text = percent.ToString() + "%";
                _hudBottomRightLabel.text = weapon.BladesLeft.ToString();
                if (percent <= 20)
                    _hudBottomLeftLabel.color = Color.red;
                else
                    _hudBottomLeftLabel.color = _greenColor;
                if (weapon.BladesLeft == 0)
                    _hudBottomRightLabel.color = Color.red;
                else
                    _hudBottomRightLabel.color = _greenColor;
            }
            else if (_human.Weapon is AmmoWeapon)
            {
                var weapon = (AmmoWeapon)_human.Weapon;
                if (weapon.RoundLeft == -1)
                {
                    float cooldown = weapon.GetCooldownLeft();
                    _hudBottomRightLabel.text = Util.FormatFloat(cooldown, 2);
                    _hudBottomLeftLabel.text = string.Empty;
                    if (cooldown > 0f)
                        _hudBottomRightLabel.color = Color.red;
                    else
                        _hudBottomRightLabel.color = _greenColor;
                }
                else
                {
                    _hudBottomLeftLabel.text = weapon.RoundLeft.ToString();
                    _hudBottomRightLabel.text = weapon.AmmoLeft.ToString();
                    if (weapon.RoundLeft == 0)
                        _hudBottomLeftLabel.color = Color.red;
                    else
                        _hudBottomLeftLabel.color = _greenColor;
                    if (weapon.AmmoLeft == 0)
                        _hudBottomRightLabel.color = Color.red;
                    else
                        _hudBottomRightLabel.color = _greenColor;
                }
            }
        }

        private string GetKeybindStrings()
        {
            string str = "";
            if (SettingsManager.UISettings.ShowInterpolation.Value)
            {
                var gameManager = (InGameManager)SceneLoader.CurrentGameManager;
                if (gameManager.CurrentCharacter != null && gameManager.CurrentCharacter is Human)
                {
                    if (((Human)gameManager.CurrentCharacter).Cache.Rigidbody.interpolation == RigidbodyInterpolation.Interpolate)
                        str = "\n" + "Interpolation: " + ChatManager.GetColorString("ON", ChatTextColor.System);
                    else
                        str = "\n" + "Interpolation: " + ChatManager.GetColorString("OFF", ChatTextColor.System);
                }
            }
            if (!SettingsManager.UISettings.ShowKeybindTip.Value)
                return str;
            var settings = SettingsManager.InputSettings;
            str += "\n" + "Pause: " + ChatManager.GetColorString(settings.General.Pause.ToString(), ChatTextColor.System);
            str += "\n" + "Scoreboard: " + ChatManager.GetColorString(settings.General.ToggleScoreboard.ToString(), ChatTextColor.System);
            str += "\n" + "Change Char: " + ChatManager.GetColorString(settings.General.ChangeCharacter.ToString(), ChatTextColor.System);
            return str;
        }

        private void HideAllMenus()
        {
            HideAllPopups();
            EmoteHandler.SetEmoteWheel(false);
            ItemHandler.SetItemWheel(false);
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();
            _settingsPopup = ElementFactory.CreateHeadedPanel<SettingsPopup>(transform).GetComponent<BasePopup>();
            _pausePopup = ElementFactory.CreateHeadedPanel<PausePopup>(transform).GetComponent<PausePopup>();
            _createGamePopup = ElementFactory.CreateHeadedPanel<CreateGamePopup>(transform).GetComponent<CreateGamePopup>();
            _popups.Add(_settingsPopup);
            _popups.Add(_pausePopup);
            _popups.Add(_createGamePopup);
            _allPausePopups.Add(_settingsPopup);
            _allPausePopups.Add(_pausePopup);
            _allPausePopups.Add(_createGamePopup);
        }
    }
}

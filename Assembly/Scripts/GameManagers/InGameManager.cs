using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using System.Diagnostics;
using Characters;
using Settings;
using CustomLogic;
using Effects;
using Map;
using System.Collections;
using GameProgress;

namespace GameManagers
{
    class InGameManager : BaseGameManager
    {
        private SkyboxCustomSkinLoader _skyboxCustomSkinLoader;
        private GeneralInputSettings _generalInputSettings;
        private InGameMenu _inGameMenu;
        public HashSet<Human> Humans = new HashSet<Human>();
        public HashSet<BasicTitan> Titans = new HashSet<BasicTitan>();
        public HashSet<BaseShifter> Shifters = new HashSet<BaseShifter>();
        public bool IsEnding;
        public float EndTimeLeft;
        public GameState State = GameState.Loading;
        public BaseCharacter CurrentCharacter;
        private bool _gameSettingsLoaded = false;
        public static Dictionary<int, PlayerInfo> AllPlayerInfo = new Dictionary<int, PlayerInfo>();
        public static HashSet<int> MuteEmote = new HashSet<int>();
        public static HashSet<int> MuteText = new HashSet<int>();
        public static PlayerInfo MyPlayerInfo = new PlayerInfo();
        private static bool _needSendPlayerInfo;
        public bool HasSpawned = false;

        public HashSet<BaseCharacter> GetAllCharacters()
        {
            var characters = new HashSet<BaseCharacter>();
            foreach (var human in Humans)
            {
                if (human != null && !human.Dead)
                    characters.Add(human);
            }
            foreach (var titan in Titans)
            {
                if (titan != null && !titan.Dead)
                    characters.Add(titan);
            }
            foreach (var shifter in Shifters)
            {
                if (shifter != null && !shifter.Dead)
                    characters.Add(shifter);
            }
            return characters;
        }

        public static void RestartGame()
        {
            if (!PhotonNetwork.isMasterClient)
                return;
            PhotonNetwork.DestroyAll();
            RPCManager.PhotonView.RPC("RestartGameRPC", PhotonTargets.All, new object[0]);
        }

        public static void OnRestartGameRPC(PhotonMessageInfo info)
        {
            if (!info.sender.isMasterClient)
                return;
            ResetRoundPlayerProperties();
            if (!PhotonNetwork.offlineMode)
                ChatManager.AddLine("Master client has restarted the game.", ChatTextColor.System);
            SceneLoader.LoadScene(SceneName.InGame);
        }

        public static void LeaveRoom()
        {
            ResetPersistentPlayerProperties();
            if (PhotonNetwork.isMasterClient)
                PhotonNetwork.DestroyAll();
            if (PhotonNetwork.connected)
                PhotonNetwork.Disconnect();
            SettingsManager.InGameCurrent.SetDefault();
            SettingsManager.InGameUI.SetDefault();
            SettingsManager.InGameCharacterSettings.SetDefault();
            SceneLoader.LoadScene(SceneName.MainMenu);
            MaterialCache.Clear();
        }

        public void OnLeftRoom()
        {
            if (PhotonNetwork.connected)
            {
                LeaveRoom();
                MainMenuGameManager.JustLeftRoom = true;
            }
        }

        public static void OnJoinRoom()
        {
            ResetPersistentPlayerProperties();
            ResetPlayerInfo();
            _needSendPlayerInfo = true;
            if (PhotonNetwork.offlineMode)
                ChatManager.AddLine("Welcome to single player. \nType /help for a list of commands.", ChatTextColor.System);
            else
                ChatManager.AddLine("Welcome to " + PhotonNetwork.room.GetStringProperty(RoomProperty.Name).Trim() + ". \nType /help for a list of commands.", 
                    ChatTextColor.System);
            SceneLoader.LoadScene(SceneName.InGame);
        }

        public void RegisterMainCharacterDie()
        {
            UpdateRoundPlayerProperties();
            if (CurrentCharacter == null)
                return;
            PhotonNetwork.player.SetCustomProperty(PlayerProperty.Deaths, PhotonNetwork.player.GetIntProperty(PlayerProperty.Deaths) + 1);
        }

        public void RegisterMainCharacterKill(BaseCharacter victim)
        {
            if (CurrentCharacter == null)
                return;
            var killWeapon = KillWeapon.Other;
            if (CurrentCharacter is Human)
            {
                var human = (Human)CurrentCharacter;
                if (human.Setup.Weapon == HumanWeapon.AHSS)
                    killWeapon = KillWeapon.AHSS;
                else if (human.Setup.Weapon == HumanWeapon.Blade)
                    killWeapon = KillWeapon.Blade;
                else if (human.Setup.Weapon == HumanWeapon.Thunderspear)
                    killWeapon = KillWeapon.Thunderspear;
                else if (human.Setup.Weapon == HumanWeapon.APG)
                    killWeapon = KillWeapon.APG;
            }
            else if (CurrentCharacter is BasicTitan)
                killWeapon = KillWeapon.Titan;
            else if (CurrentCharacter is BaseShifter)
                killWeapon = KillWeapon.Shifter;
            if (victim is Human)
                GameProgressManager.RegisterHumanKill(CurrentCharacter.gameObject, (Human)victim, killWeapon);
            else if (victim is BasicTitan)
                GameProgressManager.RegisterTitanKill(CurrentCharacter.gameObject, (BasicTitan)victim, killWeapon);
            var properties = new Dictionary<string, object>
            {
                { PlayerProperty.Kills, PhotonNetwork.player.GetIntProperty(PlayerProperty.Kills) + 1 }
            };
            PhotonNetwork.player.SetCustomProperties(properties);
        }

        public void RegisterMainCharacterDamage(BaseCharacter victim, int damage)
        {
            if (CurrentCharacter == null)
                return;
            var killWeapon = KillWeapon.Other;
            if (CurrentCharacter is Human)
            {
                var human = (Human)CurrentCharacter;
                if (human.Setup.Weapon == HumanWeapon.AHSS)
                    killWeapon = KillWeapon.AHSS;
                else if (human.Setup.Weapon == HumanWeapon.Blade)
                    killWeapon = KillWeapon.Blade;
                else if (human.Setup.Weapon == HumanWeapon.Thunderspear)
                    killWeapon = KillWeapon.Thunderspear;
                else if (human.Setup.Weapon == HumanWeapon.APG)
                    killWeapon = KillWeapon.APG;
            }
            else if (CurrentCharacter is BasicTitan)
                killWeapon = KillWeapon.Titan;
            else if (CurrentCharacter is BaseShifter)
                killWeapon = KillWeapon.Shifter;
            GameProgressManager.RegisterDamage(CurrentCharacter.gameObject, victim.gameObject, killWeapon, damage);
            var properties = new Dictionary<string, object>
            {
                { PlayerProperty.TotalDamage, PhotonNetwork.player.GetIntProperty(PlayerProperty.TotalDamage) + damage },
                { PlayerProperty.HighestDamage, Mathf.Max(PhotonNetwork.player.GetIntProperty(PlayerProperty.HighestDamage), damage) }
            };
            PhotonNetwork.player.SetCustomProperties(properties);
        }

        public void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (!AllPlayerInfo.ContainsKey(player.ID))
                AllPlayerInfo.Add(player.ID, new PlayerInfo());
            RPCManager.PhotonView.RPC("PlayerInfoRPC", player, new object[] { StringCompression.Compress(MyPlayerInfo.SerializeToJsonString()) });
            if (PhotonNetwork.isMasterClient)
            {
                RPCManager.PhotonView.RPC("GameSettingsRPC", player, new object[] { StringCompression.Compress(SettingsManager.InGameCurrent.SerializeToJsonString()) });
                string motd = SettingsManager.InGameCurrent.Misc.Motd.Value;
                if (motd != string.Empty)
                    ChatManager.SendChat("MOTD: " + motd, player, ChatTextColor.System);
            }
        }

        public void OnNotifyPlayerJoined(PhotonPlayer player)
        {
            CustomLogicManager.Evaluator.OnPlayerJoin(player);
            string line = player.GetCustomProperty(PlayerProperty.Name) + ChatManager.GetColorString(" has joined the room.", ChatTextColor.System);
            ChatManager.AddLine(line);
        }

        public void OnPhotonPlayerDisconnected(PhotonPlayer player)
        {
            string line = player.GetCustomProperty(PlayerProperty.Name) + ChatManager.GetColorString(" has left the room.", ChatTextColor.System);
            ChatManager.AddLine(line);
            if (CustomLogicManager.Evaluator != null)
                CustomLogicManager.Evaluator.OnPlayerLeave(player);
        }

        public void OnMasterClientSwitched(PhotonPlayer newMasterClient)
        {
            ChatManager.AddLine("Master client has switched to " + newMasterClient.GetCustomProperty(PlayerProperty.Name) + ".", ChatTextColor.System);
            if (PhotonNetwork.isMasterClient)
            {
                PhotonNetwork.Instantiate("RCAsset/RPCManagerPrefab", Vector3.zero, Quaternion.identity, 0);
                RestartGame();
            }
        }

        public static void OnPlayerInfoRPC(byte[] data, PhotonMessageInfo info)
        {
            if (!AllPlayerInfo.ContainsKey(info.sender.ID))
                AllPlayerInfo.Add(info.sender.ID, new PlayerInfo());
            AllPlayerInfo[info.sender.ID].DeserializeFromJsonString(StringCompression.Decompress(data));
        }

        public static void OnGameSettingsRPC(byte[] data, PhotonMessageInfo info)
        {
            if (!info.sender.isMasterClient)
                return;
            SettingsManager.InGameCurrent.DeserializeFromJsonString(StringCompression.Decompress(data));
            ((InGameManager)SceneLoader.CurrentGameManager)._gameSettingsLoaded = true;
            if (SettingsManager.InGameCurrent.Misc.EndlessRespawnEnabled.Value)
            {
                var gameManager = (InGameManager)SceneLoader.CurrentGameManager;
                gameManager.StartCoroutine(gameManager.RespawnForever(SettingsManager.InGameCurrent.Misc.EndlessRespawnTime.Value));
            }
        }

        public void SpawnPlayer(bool force)
        {
            var settings = SettingsManager.InGameCharacterSettings;
            var character = settings.CharacterType.Value;
            Vector3 position = Vector3.zero;
            if (PhotonNetwork.player.HasSpawnPoint())
                position = PhotonNetwork.player.GetSpawnPoint();
            else if (character == PlayerCharacter.Human)
                position = GetHumanSpawnPoint();
            else
                position = GetTitanSpawnPoint();
            SpawnPlayerAt(force, position);
        }

        public void SpawnPlayerShifterAt(string shifterName, float liveTime, Vector3 position)
        {
            if (shifterName == "Annie")
            {
                var shifter = (AnnieShifter)CharacterSpawner.Spawn(CharacterPrefabs.AnnieShifter, position, Quaternion.identity);
                shifter.Init(false, GetPlayerTeam(false), null, liveTime);
                CurrentCharacter = shifter;
            }
            else if (shifterName == "Eren")
            {
                var shifter = (ErenShifter)CharacterSpawner.Spawn(CharacterPrefabs.ErenShifter, position, Quaternion.identity);
                shifter.Init(false, GetPlayerTeam(false), null, liveTime);
                CurrentCharacter = shifter;
            }
        }

        public void SpawnPlayerAt(bool force, Vector3 position)
        {
            var settings = SettingsManager.InGameCharacterSettings;
            var character = settings.CharacterType.Value;
            var miscSettings = SettingsManager.InGameCurrent.Misc;
            if (settings.ChooseStatus.Value != (int)ChooseCharacterStatus.Chosen)
                return;
            if (CurrentCharacter != null && !CurrentCharacter.Dead && !force)
                return;
            if (CurrentCharacter != null && !CurrentCharacter.Dead)
                CurrentCharacter.GetKilled("");
            UpdatePlayerName();
            List<string> characters = new List<string>();
            if (miscSettings.AllowAHSS.Value || miscSettings.AllowBlades.Value || miscSettings.AllowThunderspears.Value || miscSettings.AllowAPG.Value)
                characters.Add(PlayerCharacter.Human);
            if (miscSettings.AllowPlayerTitans.Value)
                characters.Add(PlayerCharacter.Titan);
            if (miscSettings.AllowShifters.Value)
                characters.Add(PlayerCharacter.Shifter);
            if (characters.Count == 0)
                characters.Add(PlayerCharacter.Human);
            if (!characters.Contains(character))
                character = characters[0];
            if (character == PlayerCharacter.Human)
            {
                List<string> loadouts = new List<string>();
                if (miscSettings.AllowBlades.Value)
                    loadouts.Add(HumanLoadout.Blades);
                if (miscSettings.AllowAHSS.Value)
                    loadouts.Add(HumanLoadout.AHSS);
                if (miscSettings.AllowAPG.Value)
                    loadouts.Add(HumanLoadout.APG);
                if (miscSettings.AllowThunderspears.Value)
                    loadouts.Add(HumanLoadout.Thunderspears);
                if (loadouts.Count == 0)
                    loadouts.Add(HumanLoadout.Blades);
                if (!loadouts.Contains(settings.Loadout.Value))
                    settings.Loadout.Value = loadouts[0];
                List<string> specials = HumanSpecials.GetSpecialNames(settings.Loadout.Value, miscSettings.AllowShifterSpecials.Value);
                if (!specials.Contains(settings.Special.Value))
                    settings.Special.Value = specials[0];
                var human = (Human)CharacterSpawner.Spawn(CharacterPrefabs.Human, position, Quaternion.identity);
                human.Init(false, GetPlayerTeam(false), SettingsManager.InGameCharacterSettings);
                CurrentCharacter = human;
            }
            else if (character == PlayerCharacter.Shifter)
                SpawnPlayerShifterAt(settings.Loadout.Value, 0f, position);
            else if (character == PlayerCharacter.Titan)
            {
                int[] combo = BasicTitanSetup.GetRandomBodyHeadCombo();
                string prefab = CharacterPrefabs.BasicTitanPrefix + combo[0].ToString();
                var titan = (BasicTitan)CharacterSpawner.Spawn(prefab, position, Quaternion.identity);
                titan.Init(false, GetPlayerTeam(true), null, combo[1]);
                SetupTitan(titan);
                if (settings.Loadout.Value == "Small")
                    titan.SetSize(1f);
                else if (settings.Loadout.Value == "Medium")
                    titan.SetSize(2f);
                else if (settings.Loadout.Value == "Large")
                    titan.SetSize(3f);
                CurrentCharacter = titan;
            }
            HasSpawned = true;
            PhotonNetwork.player.SetCustomProperty(PlayerProperty.CharacterViewId, CurrentCharacter.Cache.PhotonView.viewID);
            RPCManager.PhotonView.RPC("NotifyPlayerSpawnRPC", PhotonTargets.All, new object[] { CurrentCharacter.Cache.PhotonView.viewID });
            UpdateRoundPlayerProperties();
        }

        private Vector3 GetHumanSpawnPoint()
        {
            if (SettingsManager.InGameCurrent.Misc.PVP.Value == (int)PVPMode.Team)
            {
                List<string> tags;
                if (SettingsManager.InGameCharacterSettings.Team.Value == TeamInfo.Blue)
                    tags = new List<string>() { MapTags.HumanSpawnPointBlue, MapTags.HumanSpawnPoint, MapTags.HumanSpawnPointRed };
                else
                    tags = new List<string>() { MapTags.HumanSpawnPointRed, MapTags.HumanSpawnPoint, MapTags.HumanSpawnPointBlue };
                return MapManager.GetRandomTagsPosition(tags, Vector3.zero);
            }
            else
            {
                List<string> tags = new List<string>() { MapTags.HumanSpawnPoint, MapTags.HumanSpawnPointBlue, MapTags.HumanSpawnPointRed};
                return MapManager.GetRandomTagsPosition(tags, Vector3.zero);
            }
        }

        private Vector3 GetTitanSpawnPoint()
        {
            return MapManager.GetRandomTagPosition(MapTags.TitanSpawnPoint, Vector3.zero);
        }

        private string GetPlayerTeam(bool titan)
        {
            if (SettingsManager.InGameCurrent.Misc.PVP.Value == (int)PVPMode.Team)
                return SettingsManager.InGameCharacterSettings.Team.Value;
            else if (SettingsManager.InGameCurrent.Misc.PVP.Value == (int)PVPMode.FFA)
                return TeamInfo.None;
            else if (titan)
                return TeamInfo.Titan;
            else
                return TeamInfo.Human;
        }

        public BasicTitan SpawnAITitan(string type)
        {
            Vector3 position = GetTitanSpawnPoint();
            return SpawnAITitanAt(type, position);
        }

        public List<BasicTitan> SpawnAITitans(string type, int count)
        {
            List<Vector3> positions = MapManager.GetRandomTagPositions(MapTags.TitanSpawnPoint, Vector3.zero, count);
            List<BasicTitan> titans = new List<BasicTitan>();
            for (int i = 0; i < count; i++)
                titans.Add(SpawnAITitanAt(type, positions[i]));
            return titans;
        }

        public BasicTitan SpawnAITitanAt(string type, Vector3 position)
        {
            if (type == "Default")
            {
                var settings = SettingsManager.InGameCurrent.Titan;
                if (settings.TitanSpawnEnabled.Value)
                {
                    float roll = Random.Range(0f, 1f);
                    float normal = settings.TitanSpawnNormal.Value / 100f;
                    float abnormal = normal + settings.TitanSpawnAbnormal.Value / 100f;
                    float jumper = abnormal + settings.TitanSpawnJumper.Value / 100f;
                    float crawler = jumper + settings.TitanSpawnCrawler.Value / 100f;
                    float punk = crawler + settings.TitanSpawnPunk.Value / 100f;
                    if (roll < normal)
                        type = "Normal";
                    else if (roll < abnormal)
                        type = "Abnormal";
                    else if (roll < jumper)
                        type = "Jumper";
                    else if (roll < crawler)
                        type = "Crawler";
                    else if (roll < punk)
                        type = "Punk";
                }
            }
            var data = CharacterData.GetTitanAI((GameDifficulty)SettingsManager.InGameCurrent.General.Difficulty.Value, type);
            int[] combo = BasicTitanSetup.GetRandomBodyHeadCombo(data);
            string prefab = CharacterPrefabs.BasicTitanPrefix + combo[0].ToString();
            var titan = (BasicTitan)CharacterSpawner.Spawn(prefab, position, Quaternion.identity);
            titan.Init(true, TeamInfo.Titan, data, combo[1]);
            SetupTitan(titan);
            return titan;
        }

        public void SetupTitan(BasicTitan titan)
        {
            var settings = SettingsManager.InGameCurrent.Titan;
            if (settings.TitanSizeEnabled.Value)
            {
                float size = Random.Range(settings.TitanSizeMin.Value, settings.TitanSizeMax.Value);
                titan.SetSize(size);
            }
            else
            {
                float size = Random.Range(1f, 3f);
                titan.SetSize(size);
            }
            if (settings.TitanHealthMode.Value > 0)
            {
                if (settings.TitanHealthMode.Value == 1)
                {
                    int health = Random.Range(settings.TitanHealthMin.Value, settings.TitanHealthMax.Value);
                    titan.SetHealth(health);
                }
                else if (settings.TitanHealthMode.Value == 2)
                {
                    float scale = Mathf.Clamp((titan.Size - 1f) / 2f, 0f, 1f);
                    int health = (int)(scale * (settings.TitanHealthMax.Value - settings.TitanHealthMin.Value) + settings.TitanHealthMin.Value);
                    health = Mathf.Max(health, 1);
                    titan.SetHealth(health);
                }
            }
        }

        public BaseShifter SpawnAIShifter(string type)
        {
            Vector3 position = GetTitanSpawnPoint();
            return SpawnAIShifterAt(type, position);
        }

        public BaseShifter SpawnAIShifterAt(string type, Vector3 position)
        {
            string prefab = "";
            if (type == "Annie")
                prefab = CharacterPrefabs.AnnieShifter;
            if (prefab == "")
                return null;
            var shifter = (BaseShifter)CharacterSpawner.Spawn(prefab, position, Quaternion.identity);
            var data = CharacterData.GetShifterAI((GameDifficulty)SettingsManager.InGameCurrent.General.Difficulty.Value, type);
            shifter.Init(true, TeamInfo.Titan, data, 0f);
            return shifter;
        }

        public static void OnSetLabelRPC(string label, string message, float time, PhotonMessageInfo info)
        {
            if (info.sender != PhotonNetwork.masterClient)
                return;
            SetLabel(label, message, time);
        }

        public static void SetLabel(string label, string message, float time = 0f)
        {
            var menu = (InGameMenu)UIManager.CurrentMenu;
            menu.SetLabel(label, message, time);
        }

        public void EndGame(float time, PhotonMessageInfo info)
        {
            if (info.sender != PhotonNetwork.masterClient)
                return;
            if (!IsEnding)
            {
                IsEnding = true;
                EndTimeLeft = time;
                if (PhotonNetwork.isMasterClient)
                    StartCoroutine(WaitAndEndGame(time));
                if (SettingsManager.UISettings.GameFeed.Value)
                {
                    float timestamp = CustomLogicManager.Evaluator.CurrentTime;
                    string feed = ChatManager.GetColorString("(" + Util.FormatFloat(timestamp, 2) + ")", ChatTextColor.System) + " Round ended.";
                    ChatManager.AddFeed(feed);
                }
            }
        }

        private IEnumerator WaitAndEndGame(float time)
        {
            yield return new WaitForSeconds(time);
            RestartGame();
        }

        private static void ResetPersistentPlayerProperties()
        {
            PhotonNetwork.player.customProperties.Clear();
            var properties = new Dictionary<string, object>
            {
                { PlayerProperty.Name, MyPlayerInfo.Profile.Name.Value.HexColor() },
                { PlayerProperty.Guild, MyPlayerInfo.Profile.Guild.Value.HexColor() },
                { PlayerProperty.Team, null },
                { PlayerProperty.CharacterViewId, -1 },
                { PlayerProperty.CustomMapHash, null },
                { PlayerProperty.CustomLogicHash, null },
                { PlayerProperty.Status, null },
                { PlayerProperty.Character, null },
                { PlayerProperty.Loadout, null },
                { PlayerProperty.Kills, 0 },
                { PlayerProperty.Deaths, 0 },
                { PlayerProperty.HighestDamage, 0 },
                { PlayerProperty.TotalDamage, 0 },
                { PlayerProperty.SpawnPoint, "null" }
            };
            PhotonNetwork.player.SetCustomProperties(properties);
        }

        private static void ResetRoundPlayerProperties()
        {
            if (SettingsManager.InGameCurrent.Misc.ClearKDROnRestart.Value)
            {
                var kdrProperties = new Dictionary<string, object>
                {
                    { PlayerProperty.Kills, 0 },
                    { PlayerProperty.Deaths, 0 },
                    { PlayerProperty.HighestDamage, 0 },
                    { PlayerProperty.TotalDamage, 0 }
                };
                PhotonNetwork.player.SetCustomProperties(kdrProperties);
            }
            var properties = new Dictionary<string, object>
            {
                { PlayerProperty.Status, PlayerStatus.Spectating },
                { PlayerProperty.CharacterViewId, -1 },
                { PlayerProperty.SpawnPoint, "null" }
            };
            PhotonNetwork.player.SetCustomProperties(properties);
        }

        public static void UpdatePlayerName()
        {
            string name = MyPlayerInfo.Profile.Name.Value.HexColor();
            if (SettingsManager.InGameCurrent.Misc.PVP.Value == (int)PVPMode.Team)
            {
                if (SettingsManager.InGameCharacterSettings.Team.Value == TeamInfo.Blue)
                    name = ChatManager.GetColorString(name, ChatTextColor.TeamBlue);
                else if (SettingsManager.InGameCharacterSettings.Team.Value == TeamInfo.Red)
                    name = ChatManager.GetColorString(name, ChatTextColor.TeamRed);
            }
            PhotonNetwork.player.SetCustomProperty(PlayerProperty.Name, name);
        }

        public static void UpdateRoundPlayerProperties()
        {
            var manager = (InGameManager)SceneLoader.CurrentGameManager;
            string status;
            if (SettingsManager.InGameCharacterSettings.ChooseStatus.Value != (int)ChooseCharacterStatus.Chosen)
                status = PlayerStatus.Spectating;
            else if (manager.CurrentCharacter != null && !manager.CurrentCharacter.Dead)
                status = PlayerStatus.Alive;
            else
                status = PlayerStatus.Dead;
           
            var properties = new Dictionary<string, object>
            {
                { PlayerProperty.Status, status },
                { PlayerProperty.Character, SettingsManager.InGameCharacterSettings.CharacterType.Value },
                { PlayerProperty.Loadout, SettingsManager.InGameCharacterSettings.Loadout.Value },
                { PlayerProperty.Team, SettingsManager.InGameCharacterSettings.Team.Value }
            };
            PhotonNetwork.player.SetCustomProperties(properties);
        }

        private static void ResetPlayerInfo()
        {
            AllPlayerInfo.Clear();
            MuteEmote.Clear();
            MuteText.Clear();
            PlayerInfo myPlayerInfo = new PlayerInfo();
            myPlayerInfo.Profile.Copy(SettingsManager.ProfileSettings);
            AllPlayerInfo.Add(PhotonNetwork.player.ID, myPlayerInfo);
            MyPlayerInfo = myPlayerInfo;
        }

        protected override void Awake()
        {
            _skyboxCustomSkinLoader = gameObject.AddComponent<SkyboxCustomSkinLoader>();
            _generalInputSettings = SettingsManager.InputSettings.General;
            ResetRoundPlayerProperties();
            if (PhotonNetwork.isMasterClient)
            {
                SettingsManager.InGameCurrent.Copy(SettingsManager.InGameUI);
                PhotonNetwork.Instantiate("RCAsset/RPCManagerPrefab", Vector3.zero, Quaternion.identity, 0);
            }
            base.Awake();
        }

        protected override void Start()
        {
            _inGameMenu = (InGameMenu)UIManager.CurrentMenu;
            if (PhotonNetwork.isMasterClient)
            {
                RPCManager.PhotonView.RPC("GameSettingsRPC", PhotonTargets.All, new object[] { StringCompression.Compress(SettingsManager.InGameCurrent.SerializeToJsonString()) });
                var settings = SettingsManager.InGameCurrent;
                string mapName = settings.General.MapName.Value;
                string gameMode = settings.General.GameMode.Value;
                var properties = new ExitGames.Client.Photon.Hashtable
                {
                    { RoomProperty.Name, PhotonNetwork.room.GetStringProperty(RoomProperty.Name) },
                    { RoomProperty.Map, mapName },
                    { RoomProperty.GameMode, gameMode },
                    { RoomProperty.Password, PhotonNetwork.room.GetStringProperty(RoomProperty.Password) }
                };
                PhotonNetwork.room.SetCustomProperties(properties);
            }
            base.Start();
        }

        public override bool IsFinishedLoading()
        {
            return base.IsFinishedLoading() && _gameSettingsLoaded;
        }

        private void Update()
        {
            if (State != GameState.Loading)
                UpdateInput();
            UpdateCleanCharacters();
            EndTimeLeft -= Time.deltaTime;
            EndTimeLeft = Mathf.Max(EndTimeLeft, 0f);
        }

        protected override void OnFinishLoading()
        {
            base.OnFinishLoading();
            if (CustomLogicManager.Logic == BuiltinLevels.UseMapLogic)
                CustomLogicManager.Logic = MapManager.MapScript.Logic;
            if (_needSendPlayerInfo)
            {
                RPCManager.PhotonView.RPC("PlayerInfoRPC", PhotonTargets.Others, new object[] { StringCompression.Compress(MyPlayerInfo.SerializeToJsonString()) });
                if (!PhotonNetwork.isMasterClient)
                    RPCManager.PhotonView.RPC("NotifyPlayerJoinedRPC", PhotonTargets.Others, new object[0]);
                _needSendPlayerInfo = false;
            }
            ((InGameMenu)UIManager.CurrentMenu).UpdateLoading(1f, true);
            if (State == GameState.Loading)
                State = GameState.Playing;
            if (SettingsManager.InGameCharacterSettings.ChooseStatus.Value == (int)ChooseCharacterStatus.Choosing)
                _inGameMenu.SetCharacterMenu(true);
            CustomLogicManager.StartLogic(SettingsManager.InGameCurrent.Mode.Current);
            SpawnPlayer(false);
            if (SettingsManager.UISettings.GameFeed.Value)
            {
                float time = CustomLogicManager.Evaluator.CurrentTime;
                string feed = ChatManager.GetColorString("(" + Util.FormatFloat(time, 2) + ")", ChatTextColor.System) + " Round started.";
                ChatManager.AddFeed(feed);
            }
        }

        private void UpdateInput()
        {
            if (ChatManager.IsChatActive())
                return;
            if (_generalInputSettings.Pause.GetKeyDown())
                _inGameMenu.SetPauseMenu(true);
            if (_generalInputSettings.ChangeCharacter.GetKeyDown() && !InGameMenu.InMenu() && !CustomLogicManager.Cutscene)
            {
                if (CurrentCharacter != null && !CurrentCharacter.Dead)
                    CurrentCharacter.GetKilled("");
                SettingsManager.InGameCharacterSettings.ChooseStatus.Value = (int)ChooseCharacterStatus.Choosing;
                _inGameMenu.SetCharacterMenu(true);
            }
            if (_generalInputSettings.RestartGame.GetKeyDown() && PhotonNetwork.isMasterClient)
                RestartGame();
            if (_generalInputSettings.TapScoreboard.Value)
            {
                if (_generalInputSettings.ToggleScoreboard.GetKeyDown())
                    _inGameMenu.ToggleScoreboardMenu();
            }
            else
            {
                if (_generalInputSettings.ToggleScoreboard.GetKey())
                    _inGameMenu.SetScoreboardMenu(true);
                else
                    _inGameMenu.SetScoreboardMenu(false);
            }
        }

        private void UpdateCleanCharacters()
        {
            Humans = Util.RemoveNullOrDead(Humans);
            Titans = Util.RemoveNullOrDead(Titans);
            Shifters = Util.RemoveNullOrDeadShifters(Shifters);
        }

        private IEnumerator RespawnForever(float delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                SpawnPlayer(false);
            }
        }
    }

    public enum GameState
    {
        Loading,
        Playing,
        Paused
    }
}

using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using System.Diagnostics;
using Settings;
using Anticheat;

namespace GameManagers
{
    class ChatManager : MonoBehaviour
    {
        private static ChatManager _instance;
        public static List<string> Lines = new List<string>();
        public static List<string> FeedLines = new List<string>();
        private static readonly int MaxLines = 30;
        public static Dictionary<ChatTextColor, string> ColorTags = new Dictionary<ChatTextColor, string>();

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
        }
        
        public static void Reset()
        {
            Lines.Clear();
            FeedLines.Clear();
            LoadTheme();
        }

        public static void Clear()
        {
            Lines.Clear();
            FeedLines.Clear();
            GetChatPanel().Sync();
            var feedPanel = GetFeedPanel();
            if (feedPanel != null)
                feedPanel.Sync();
        }

        public static bool IsChatActive()
        {
            return GetChatPanel().IsInputActive();
        }

        public static bool IsChatAvailable()
        {
            return SceneLoader.SceneName == SceneName.InGame && UIManager.CurrentMenu != null && UIManager.CurrentMenu is InGameMenu;
        }

        public static void SendChatAll(string message, ChatTextColor color = ChatTextColor.Default)
        {
            message = GetColorString(message, color);
            RPCManager.PhotonView.RPC("ChatRPC", PhotonTargets.All, new object[] { message });
        }

        public static void SendChat(string message, PhotonPlayer player, ChatTextColor color = ChatTextColor.Default)
        {
            message = GetColorString(message, color);
            RPCManager.PhotonView.RPC("ChatRPC", player, new object[] { message });
        }

        public static void OnChatRPC(string message, PhotonMessageInfo info)
        {
            if (InGameManager.MuteText.Contains(info.sender.ID))
                return;
            AddLine(message, info.sender.ID);
        }

        public static void AddLine(string line, ChatTextColor color = ChatTextColor.Default)
        {
            AddLine(GetColorString(line, color));
        }

        public static void AddLine(string line, int senderID)
        {
            line = GetIDString(senderID) + line;
            AddLine(line);
        }

        public static void AddLine(string line)
        {
            line = line.FilterSizeTag();
            Lines.Add(line);
            if (Lines.Count > MaxLines)
                Lines.RemoveAt(0);
            if (IsChatAvailable())
            {
                GetChatPanel().AddLine(line);
            }
        }

        public static void AddFeed(string line)
        {
            if (!IsChatAvailable())
                return;
            var feed = GetFeedPanel();
            if (feed == null)
            {
                AddLine(line);
                return;
            }
            line = line.FilterSizeTag();
            FeedLines.Add(line);
            if (FeedLines.Count > MaxLines)
                FeedLines.RemoveAt(0);
            feed.AddLine(line);
        }

        protected static void LoadTheme()
        {
            ColorTags.Clear();
            foreach (ChatTextColor color in Util.EnumToList<ChatTextColor>())
            {
                Color c = UIManager.GetThemeColor("ChatPanel", "TextColor", color.ToString());
                ColorTags.Add(color, string.Format("{0:X2}{1:X2}{2:X2}", (int)(c.r * 255), (int)(c.g * 255), (int)(c.b * 255)));
            }
        }

        public static void HandleInput(string input)
        {
            if (input == string.Empty)
                return;
            if (input.StartsWith("/"))
            {
                if (input.Length == 1)
                    return;
                string[] args = input.Substring(1).Split(' ');
                if (args.Length > 0)
                    HandleCommand(args);
            }
            else
            {
                string name = PhotonNetwork.player.GetStringProperty(PlayerProperty.Name);
                SendChatAll(name + ": " + input);
            }
        }

        private static void HandleCommand(string[] args)
        {
            if (args[0] == "restart")
            {
                if (CheckMC())
                    InGameManager.RestartGame();
            }
            else if (args[0] == "clear")
                Clear();
            else if (args[0] == "reviveall")
            {
                if (CheckMC())
                {
                    RPCManager.PhotonView.RPC("SpawnPlayerRPC", PhotonTargets.All, new object[] { false });
                    SendChatAll("All players have been revived by master client.", ChatTextColor.System);
                }
            }
            else if (args[0] == "revive")
            {
                if (CheckMC())
                {
                    var player = GetPlayer(args);
                    if (player != null)
                    {
                        RPCManager.PhotonView.RPC("SpawnPlayerRPC", player, new object[] { false });
                        SendChat("You have been revived by master client.", player, ChatTextColor.System);
                        AddLine(player.GetStringProperty(PlayerProperty.Name) + " has been revived.", ChatTextColor.System);
                    }
                }
            }
            else if (args[0] == "pm")
            {
                var player = GetPlayer(args);
                if (args.Length > 2 && player != null)
                {
                    SendChat("From " + PhotonNetwork.player.GetStringProperty(PlayerProperty.Name) + ": " + args[2], player);
                    AddLine("To " + player.GetStringProperty(PlayerProperty.Name) + ": " + args[2]);
                }
            }
            else if (args[0] == "kick")
            {
                if (CheckMC())
                {
                    var player = GetPlayer(args);
                    if (player != null)
                        KickPlayer(player);
                }
            }
            else if (args[0] == "maxplayers")
            {
                if (CheckMC())
                {
                    int players;
                    if (args.Length > 1 && int.TryParse(args[1], out players) && players >= 0)
                    {
                        PhotonNetwork.room.maxPlayers = players;
                        AddLine("Max players set to " + players.ToString() + ".", ChatTextColor.System);
                    }
                    else
                        AddLine("Max players must be >= 0.", ChatTextColor.Error);
                }
            }
            else if (args[0] == "mute")
            {
                var player = GetPlayer(args);
                if (player != null)
                {
                    MutePlayer(player, true);
                    MutePlayer(player, false);
                }
            }
            else if (args[0] == "unmute")
            {
                var player = GetPlayer(args);
                if (player != null)
                {
                    UnmutePlayer(player, true);
                    UnmutePlayer(player, false);
                }
            }
            else if (args[0] == "help")
            {
                string help = "----Command list----" + "\n";
                help += "/restart: Restart the game\n";
                help += "/clear: Clear the chat\n";
                help += "/revive [ID]: Revive the player with ID\n";
                help += "/reviveall: Revive all players\n";
                help += "/pm [ID]: Private message player with ID\n";
                help += "/kick [ID]: Kick the player with ID\n";
                help += "/mute [ID]: Mute player with ID\n";
                help += "/unmute[ID]: Unmute player with ID\n";
                help += "/maxplayers [num]: Sets max players.";
                AddLine(help, ChatTextColor.System);
            }
        }

        public static void KickPlayer(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient && player != PhotonNetwork.player)
            {
                AnticheatManager.KickPlayer(player);
                SendChatAll(player.GetStringProperty(PlayerProperty.Name) + " has been kicked.", ChatTextColor.System);
            }
        }

        public static void MutePlayer(PhotonPlayer player, bool emote)
        {
            if (player == PhotonNetwork.player)
                return;
            if (emote)
            {
                InGameManager.MuteEmote.Add(player.ID);
                AddLine(player.GetStringProperty(PlayerProperty.Name) + " has been muted (emote).", ChatTextColor.System);
            }
            else
            {
                InGameManager.MuteText.Add(player.ID);
                AddLine(player.GetStringProperty(PlayerProperty.Name) + " has been muted (chat).", ChatTextColor.System);
            }
        }

        public static void UnmutePlayer(PhotonPlayer player, bool emote)
        {
            if (player == PhotonNetwork.player)
                return;
            if (emote && InGameManager.MuteEmote.Contains(player.ID))
            {
                InGameManager.MuteEmote.Remove(player.ID);
                AddLine(player.GetStringProperty(PlayerProperty.Name) + " has been unmuted (emote).", ChatTextColor.System);
            }
            else if (!emote && InGameManager.MuteText.Contains(player.ID))
            {
                InGameManager.MuteText.Remove(player.ID);
                AddLine(player.GetStringProperty(PlayerProperty.Name) + " has been unmuted (chat).", ChatTextColor.System);
            }
        }

        private static PhotonPlayer GetPlayer(string[] args)
        {
            int id = -1;
            if (args.Length > 1 && int.TryParse(args[1], out id) && PhotonPlayer.Find(id) != null)
            {
                var player = PhotonPlayer.Find(id);
                return player;
            }
            AddLine("Invalid player ID.", ChatTextColor.Error);
            return null;
        }

        private static bool CheckMC()
        {
            if (PhotonNetwork.isMasterClient)
                return true;
            AddLine("Must be master client to use that command.", ChatTextColor.Error);
            return false;
        }

        private static ChatPanel GetChatPanel()
        {
            return ((InGameMenu)UIManager.CurrentMenu).ChatPanel;
        }

        private static FeedPanel GetFeedPanel()
        {
            return ((InGameMenu)UIManager.CurrentMenu).FeedPanel;
        }

        public static string GetIDString(int id, bool includeMC = false)
        {
            string str = "[" + id.ToString() + "] ";
            if (includeMC)
                str = "[M]" + str;
            return GetColorString(str, ChatTextColor.ID);
        }

        public static string GetColorString(string str, ChatTextColor color)
        {
            if (color == ChatTextColor.Default)
                return str;
            return "<color=#" + ColorTags[color] + ">" + str + "</color>";
        }

        private void Update()
        {
            if (IsChatAvailable() && !InGameMenu.InMenu() && !DebugConsole.Enabled)
            {
                var chatPanel = GetChatPanel();
                var key = SettingsManager.InputSettings.General.Chat;
                if (key.GetKeyDown())
                {
                    if (chatPanel.IgnoreNextActivation)
                        chatPanel.IgnoreNextActivation = false;
                    else
                        chatPanel.Activate();
                }
            }
        }
    }

    enum ChatTextColor
    {
        Default,
        ID,
        System,
        Error,
        TeamRed,
        TeamBlue
    }
}

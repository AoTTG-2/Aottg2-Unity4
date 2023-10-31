using ApplicationManagers;
using Events;
using GameManagers;
using Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Anticheat
{
    class AnticheatManager : MonoBehaviour
    {
        private static AnticheatManager _instance;
        private Dictionary<int, Dictionary<PhotonEventType, BaseEventFilter>> _IdToEventFilters = new Dictionary<int, Dictionary<PhotonEventType, BaseEventFilter>>();

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
            EventManager.OnLoadScene += OnLoadScene;
        }

        private static void OnLoadScene(SceneName sceneName)
        {
            _instance._IdToEventFilters.Clear();
        }

        public static bool CheckPhotonEvent(PhotonPlayer sender, PhotonEventType eventType, object[] data)
        {
            if (!_instance._IdToEventFilters.ContainsKey(sender.ID))
                _instance._IdToEventFilters.Add(sender.ID, new Dictionary<PhotonEventType, BaseEventFilter>());
            var filters = _instance._IdToEventFilters[sender.ID];
            if (!filters.ContainsKey(eventType))
            {
                if (eventType == PhotonEventType.Instantiate)
                    filters.Add(eventType, new InstantiateEventFilter(sender, eventType));
            }
            return filters[eventType].CheckEvent(data);
        }

        public static void KickPlayer(PhotonPlayer player, bool ban = false, string reason = "")
        {
            if (!PhotonNetwork.isMasterClient)
                return;
            if (PhotonNetwork.isMasterClient && player == PhotonNetwork.player && reason != string.Empty)
            {
                DebugConsole.Log("Attempting to ban myself for: " + reason + ", please report this to the devs.", true);
                return;
            }
            PhotonNetwork.DestroyPlayerObjects(player);
            PhotonNetwork.CloseConnection(player);
            if (reason != string.Empty)
            {
                DebugConsole.Log("Player " + player.ID.ToString() + " was autobanned. Reason:" + reason, true);
            }
        }

        public static void ServerCloseConnection(PhotonPlayer targetPlayer, bool requestIpBan, string inGameName = null)
        {
            RaiseEventOptions options = new RaiseEventOptions
            {
                TargetActors = new int[] { targetPlayer.ID }
            };
            if (requestIpBan)
            {
                ExitGames.Client.Photon.Hashtable eventContent = new ExitGames.Client.Photon.Hashtable();
                eventContent[(byte)0] = true;
                if ((inGameName != null) && (inGameName.Length > 0))
                {
                    eventContent[(byte)1] = inGameName;
                }
                PhotonNetwork.RaiseEvent(0xcb, eventContent, true, options);
            }
            else
            {
                PhotonNetwork.RaiseEvent(0xcb, null, true, options);
            }
        }
    }

    public enum PhotonEventType
    {
        Instantiate,
        RPC
    }
}
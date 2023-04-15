using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using System.Diagnostics;
using Photon;
using Settings;

namespace GameManagers
{
    class MainMenuGameManager : BaseGameManager
    {
        public static bool JustLeftRoom;

        public void OnJoinedLobby()
        {
            if (JustLeftRoom)
            {
                PhotonNetwork.Disconnect();
                JustLeftRoom = false;
            }
            else if (UIManager.CurrentMenu != null && UIManager.CurrentMenu.GetComponent<MainMenu>() != null)
                UIManager.CurrentMenu.GetComponent<MainMenu>().ShowMultiplayerRoomListPopup();
        }

        public void OnJoinedRoom()
        {
            InGameManager.OnJoinRoom();
        }
    }
}

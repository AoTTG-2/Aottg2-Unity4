using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Map;
using Effects;
using CustomLogic;
using ApplicationManagers;
using Characters;

namespace GameManagers
{
    class RPCManager: Photon.MonoBehaviour
    {
        public static PhotonView PhotonView;

        [RPC]
        public void TransferLogicRPC(byte[][] strArray, int msgNumber, int msgTotal, PhotonMessageInfo info)
        {
            CustomLogicTransfer.OnTransferLogicRPC(strArray, msgNumber, msgTotal, info);
        }

        [RPC]
        public void LoadBuiltinLogicRPC(string name, PhotonMessageInfo info)
        {
            CustomLogicManager.OnLoadBuiltinLogicRPC(name, info);
        }

        [RPC]
        public void LoadCachedLogicRPC(PhotonMessageInfo info = null)
        {
            CustomLogicManager.OnLoadCachedLogicRPC(info);
        }

        [RPC]
        public void TransferMapRPC(byte[][] strArray, int msgNumber, int msgTotal, PhotonMessageInfo info)
        {
            MapTransfer.OnTransferMapRPC(strArray, msgNumber, msgTotal, info);
        }

        [RPC]
        public void LoadBuiltinMapRPC(string category, string name, PhotonMessageInfo info)
        {
            MapManager.OnLoadBuiltinMapRPC(category, name, info);
        }

        [RPC]
        public void LoadCachedMapRPC(PhotonMessageInfo info = null)
        {
            MapManager.OnLoadCachedMapRPC(info);
        }
        
        [RPC]
        public void RestartGameRPC(PhotonMessageInfo info)
        {
            InGameManager.OnRestartGameRPC(info);
        }

        [RPC]
        public void PlayerInfoRPC(byte[] data, PhotonMessageInfo info)
        {
            InGameManager.OnPlayerInfoRPC(data, info);
        }

        [RPC]
        public void GameSettingsRPC(byte[] data, PhotonMessageInfo info)
        {
            InGameManager.OnGameSettingsRPC(data, info);
        }

        [RPC]
        public void SetWeatherRPC(byte[] currentWeatherJson, byte[] startWeatherJson, byte[] targetWeatherJson, Dictionary<int, float> targetWeatherStartTimes,
            Dictionary<int, float> targetWeatherEndTimes, float currentTime, PhotonMessageInfo info)
        {
            WeatherManager.OnSetWeatherRPC(currentWeatherJson, startWeatherJson, targetWeatherJson, targetWeatherStartTimes, targetWeatherEndTimes, currentTime, info);
        }

        [RPC]
        public void EmoteEmojiRPC(int viewId, string emoji, PhotonMessageInfo info)
        {
            EmoteHandler.OnEmoteEmojiRPC(viewId, emoji, info);
        }

        [RPC]
        public void EmoteTextRPC(int viewId, string text, PhotonMessageInfo info)
        {
            EmoteHandler.OnEmoteTextRPC(viewId, text, info);
        }

        [RPC]
        public void SpawnEffectRPC(string name, Vector3 position, Quaternion rotation, float scale, bool scaleSize, object[] settings, PhotonMessageInfo info)
        {
            EffectSpawner.OnSpawnEffectRPC(name, position, rotation, scale, scaleSize, settings, info);
        }

        [RPC]
        public void SetLabelRPC(string label, string message, float time, PhotonMessageInfo info)
        {
            InGameManager.OnSetLabelRPC(label, message, time, info);
        }

        [RPC]
        public void ShowKillFeedRPC(string killer, string victim, int score, PhotonMessageInfo info)
        {
            ((InGameMenu)UIManager.CurrentMenu).ShowKillFeed(killer, victim, score);
        }

        [RPC]
        public void EndGameRPC(float time, PhotonMessageInfo info)
        {
            ((InGameManager)SceneLoader.CurrentGameManager).EndGame(time, info);
        }

        [RPC]
        public void NotifyPlayerJoinedRPC(PhotonMessageInfo info)
        {
            ((InGameManager)SceneLoader.CurrentGameManager).OnNotifyPlayerJoined(info.sender);
        }

        [RPC]
        public void TransferNetworkViewRPC(int id, PhotonMessageInfo info)
        {
            var go = PhotonNetwork.Instantiate("RCAsset/CustomLogicPhotonSyncPrefab", Vector3.zero, Quaternion.identity, 0);
            var photonView = go.GetComponent<CustomLogicPhotonSync>();
            photonView.Init(id);
        }

        [RPC]
        public void SendMessageRPC(string message, PhotonMessageInfo info)
        {
            CustomLogicManager.Evaluator.OnNetworkMessage(info.sender, message);
        }

        [RPC]
        public void NotifyPlayerSpawnRPC(int viewId, PhotonMessageInfo info)
        {
            var view = PhotonView.Find(viewId);
            if (view != null && view.owner == info.sender  && CustomLogicManager.Evaluator != null)
            {
                var character = view.GetComponent<BaseCharacter>();
                CustomLogicManager.Evaluator.OnPlayerSpawn(info.sender, character);
            }
        }

        [RPC]
        public void SpawnPlayerRPC(bool force, PhotonMessageInfo info)
        {
            if (info.sender.isMasterClient)
            {
                ((InGameManager)SceneLoader.CurrentGameManager).SpawnPlayer(force);
            }
        }

        [RPC]
        public void SpawnPlayerAtRPC(bool force, Vector3 position, PhotonMessageInfo info)
        {
            if (info.sender.isMasterClient)
            {
                ((InGameManager)SceneLoader.CurrentGameManager).SpawnPlayerAt(force, position);
            }
        }

        [RPC]
        public void SyncCurrentTimeRPC(float time, PhotonMessageInfo info)
        {
            if (info.sender.isMasterClient && CustomLogicManager.Evaluator != null)
            {
                CustomLogicManager.Evaluator.CurrentTime = time;
            }
        }

        [RPC]
        public void ChatRPC(string message, PhotonMessageInfo info)
        {
            ChatManager.OnChatRPC(message, info);
        }

        [RPC]
        public void TestRPC(Color c)
        {
            Debug.Log(c);
        }

        void Awake()
        {
            PhotonView = GetComponent<PhotonView>();
        }
    }
}

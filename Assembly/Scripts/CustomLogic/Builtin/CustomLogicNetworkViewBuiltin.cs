﻿using System.Collections.Generic;
using UnityEngine;
using Map;
using GameManagers;
using Utility;

namespace CustomLogic
{
    class CustomLogicNetworkViewBuiltin: CustomLogicBaseBuiltin
    {
        public MapObject MapObject;
        public CustomLogicPhotonSync Sync;
        public int OwnerId = -1;
        List<CustomLogicComponentInstance> _classInstances = new List<CustomLogicComponentInstance>();
        List<object> _streamObjs;

        public CustomLogicNetworkViewBuiltin(MapObject obj): base("NetworkView")
        {
            MapObject = obj;
        }

        public void OnSecond()
        {
            if (PhotonNetwork.isMasterClient)
            {
                if (OwnerId >= 0 && OwnerId != PhotonNetwork.player.ID)
                {
                    var player = Util.FindPlayerById(OwnerId);
                    if (player == null)
                    {
                        var go = PhotonNetwork.Instantiate("RCAsset/CustomLogicPhotonSyncPrefab", Vector3.zero, Quaternion.identity, 0);
                        var photonView = go.GetComponent<CustomLogicPhotonSync>();
                        photonView.Init(MapObject.ScriptObject.Id);
                    }
                }
            }
        }

        public void RegisterComponentInstance(CustomLogicComponentInstance instance)
        {
            _classInstances.Add(instance);
        }

        public void SetSync(CustomLogicPhotonSync sync)
        {
            int oldId = OwnerId;
            Sync = sync;
            OwnerId = sync.photonView.owner.ID;
            if (oldId >= 0)
            {
                var oldPlayer = Util.FindPlayerById(oldId);
                CustomLogicPlayerBuiltin oldOwner = null;
                if (oldPlayer != null)
                    oldOwner = new CustomLogicPlayerBuiltin(oldPlayer);
                var newOwner = new CustomLogicPlayerBuiltin(Sync.photonView.owner);
                foreach (var instance in _classInstances)
                    CustomLogicManager.Evaluator.EvaluateMethod(instance, "OnNetworkTransfer", new List<object>() { oldOwner, newOwner });
            }
        }

        public void SendNetworkStream(PhotonStream stream)
        {
            _streamObjs = new List<object>();            
            foreach (var instance in _classInstances)
            {
                CustomLogicManager.Evaluator.EvaluateMethod(instance, "SendNetworkStream", new List<object>());
            }
            stream.SendNext(_streamObjs.ToArray());
        }

        public void OnNetworkStream(object[] objs)
        {
            _streamObjs = new List<object>(objs);
            foreach (var instance in _classInstances)
            {
                CustomLogicManager.Evaluator.EvaluateMethod(instance, "OnNetworkStream", new List<object>());
            }
        }

        public void OnNetworkMessage(CustomLogicPlayerBuiltin player, string message)
        {
            foreach (var instance in _classInstances)
            {
                CustomLogicManager.Evaluator.EvaluateMethod(instance, "OnNetworkMessage", new List<object>() { player, message });
            }
        }

        public override object CallMethod(string methodName, List<object> parameters)
        {
            if (methodName == "Transfer")
            {
                if (Sync.photonView.isMine)
                {
                    var player = (CustomLogicPlayerBuiltin)parameters[0];
                    if (player.Player != PhotonNetwork.player)
                    {
                        PhotonNetwork.Destroy(Sync.gameObject);
                        RPCManager.PhotonView.RPC("TransferNetworkViewRPC", player.Player, new object[] { MapObject.ScriptObject.Id });
                    }
                }
            }
            else if (methodName == "SendMessage")
            {
                var target = (CustomLogicPlayerBuiltin)parameters[0];
                string msg = (string)parameters[1];
                Sync.SendMessage(target.Player, msg);

            }
            else if (methodName == "SendMessageAll")
            {
                string msg = (string)parameters[0];
                Sync.SendMessageAll(msg);
            }
            else if (methodName == "SendMessageOthers")
            {
                string msg = (string)parameters[0];
                Sync.SendMessageOthers(msg);
            }
            else if (methodName == "SendStream")
            {
                var obj = parameters[0];
                obj = SerializeStreamObj(obj);
                _streamObjs.Add(obj);
            }
            else if (methodName == "ReceiveStream")
            {
                var obj = _streamObjs[0];
                obj = DeserializeStreamObj(obj);
                _streamObjs.RemoveAt(0);
                return obj;
            }
            return base.CallMethod(methodName, parameters);
        }

        public override object GetField(string name)
        {
            if (name == "Owner")
            {
                if (Sync == null)
                    return null;
                return new CustomLogicPlayerBuiltin(Sync.photonView.owner);
            }
            return base.GetField(name);
        }

        protected object SerializeStreamObj(object obj)
        {
            if (obj is CustomLogicVector3Builtin)
                return ((CustomLogicVector3Builtin)obj).Value;
            return obj;
        }

        protected object DeserializeStreamObj(object obj)
        {
            if (obj is Vector3)
                return new CustomLogicVector3Builtin((Vector3)obj);
            return obj;
        }
    }
}

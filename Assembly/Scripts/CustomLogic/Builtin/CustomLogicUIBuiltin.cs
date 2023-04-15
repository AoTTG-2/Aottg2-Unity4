using ApplicationManagers;
using GameManagers;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLogic
{
    class CustomLogicUIBuiltin: CustomLogicBaseBuiltin
    {
        private Dictionary<string, string> _lastSetLabels = new Dictionary<string, string>();

        public CustomLogicUIBuiltin(): base("UI")
        {
        }

        public void OnPlayerJoin(PhotonPlayer player)
        {
            if (PhotonNetwork.isMasterClient)
            {
                foreach (string key in _lastSetLabels.Keys)
                    RPCManager.PhotonView.RPC("SetLabelRPC", player, new object[] { key, _lastSetLabels[key], 0f });
            }
        }

        public override object CallMethod(string name, List<object> parameters)
        {
            if (name == "SetLabel")
            {
                string label = (string)parameters[0];
                string message = (string)parameters[1];
                InGameManager.SetLabel(label, message, 0);
            }
            else if (name == "SetLabelForTime")
            {
                string label = (string)parameters[0];
                string message = (string)parameters[1];
                float time = parameters[2].UnboxToFloat();
                InGameManager.SetLabel(label, message, time);
            }
            else if (name == "SetLabelAll")
            {
                string label = (string)parameters[0];
                string message = (string)parameters[1];
                if (PhotonNetwork.isMasterClient)
                {
                    if (!_lastSetLabels.ContainsKey(label) || message != _lastSetLabels[label])
                        RPCManager.PhotonView.RPC("SetLabelRPC", PhotonTargets.All, new object[] { label, message, 0f });
                    _lastSetLabels[label] = message;
                }
            }
            else if (name == "SetLabelForTimeAll")
            {
                string label = (string)parameters[0];
                string message = (string)parameters[1];
                float time = parameters[2].UnboxToFloat();
                if (PhotonNetwork.isMasterClient)
                {
                    if (!_lastSetLabels.ContainsKey(label) || message != _lastSetLabels[label])
                        RPCManager.PhotonView.RPC("SetLabelRPC", PhotonTargets.All, new object[] { label, message, time });
                    _lastSetLabels[label] = message;
                }
            }
            return base.CallMethod(name, parameters);
        }
    }
}

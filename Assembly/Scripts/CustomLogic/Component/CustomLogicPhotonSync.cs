using Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLogic
{
    class CustomLogicPhotonSync : Photon.MonoBehaviour
    {
        protected Vector3 _correctPosition = Vector3.zero;
        protected Quaternion _correctRotation = Quaternion.identity;
        protected Vector3 _correctVelocity = Vector3.zero;
        protected bool _syncVelocity = false;
        protected float SmoothingDelay => 5f;
        protected MapObject _mapObject;
        protected PhotonView _photonView;
        protected CustomLogicNetworkViewBuiltin _networkView;
        protected bool _inited = false;
        protected object[] _streamObjs;

        protected virtual void Awake()
        {
            _photonView = photonView;
            _photonView.observed = this;
        }

        public void Init(int mapObjectId)
        {
            _mapObject = MapLoader.IdToMapObject[mapObjectId];
            bool syncVelocity = _mapObject.GameObject.GetComponent<Rigidbody>() != null;
            _photonView.RPC("InitRPC", PhotonTargets.AllBuffered, new object[] { mapObjectId, syncVelocity });
        }

        [RPC]
        public void InitRPC(int mapObjectId, bool syncVelocity, PhotonMessageInfo info)
        {
            if (info.sender != _photonView.owner)
                return;
            _syncVelocity = syncVelocity;
            StartCoroutine(WaitAndFinishInit(mapObjectId));
        }

        public IEnumerator WaitAndFinishInit(int mapObjectId)
        {
            while (CustomLogicManager.Evaluator == null || !CustomLogicManager.Evaluator.IdToNetworkView.ContainsKey(mapObjectId))
                yield return null;
            FinishInit(mapObjectId);
        }

        private void FinishInit(int mapObjectId)
        {
            _mapObject = MapLoader.IdToMapObject[mapObjectId];
            _networkView = CustomLogicManager.Evaluator.IdToNetworkView[mapObjectId];
            _networkView.SetSync(this);
            _correctPosition = _mapObject.GameObject.transform.position;
            _correctRotation = _mapObject.GameObject.transform.rotation;
            _inited = true;
        }

        [RPC]
        public void SendMessageRPC(string message, PhotonMessageInfo info)
        {
            var player = info.sender;
            _networkView.OnNetworkMessage(new CustomLogicPlayerBuiltin(player), message);
        }

        public void SendMessage(PhotonPlayer player, string message)
        {
            _photonView.RPC("SendMessageRPC", player, new object[] { message });
        }

        public void SendMessageAll(string message)
        {
            _photonView.RPC("SendMessageRPC", PhotonTargets.All, new object[] { message });
        }

        public void SendMessageOthers(string message)
        {
            _photonView.RPC("SendMessageRPC", PhotonTargets.Others, new object[] { message });
        }

        protected virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.isWriting && _inited)
            {
                stream.SendNext(_mapObject.GameObject.transform.position);
                stream.SendNext(_mapObject.GameObject.transform.rotation);
                if (_syncVelocity)
                {
                    stream.SendNext(_mapObject.GameObject.rigidbody.velocity);
                }
                _networkView.SendNetworkStream(stream);
            }
            else
            {
                _correctPosition = (Vector3)stream.ReceiveNext();
                _correctRotation = (Quaternion)stream.ReceiveNext();
                if (_syncVelocity)
                    _correctVelocity = (Vector3)stream.ReceiveNext();
                _streamObjs = (object[])stream.ReceiveNext();
                if (_inited && _streamObjs != null && _streamObjs.Length > 0)
                    _networkView.OnNetworkStream(_streamObjs);
            }
        }

        protected virtual void Update()
        {
            if (!_photonView.isMine && _inited)
            {
                var transform = _mapObject.GameObject.transform;
                transform.position = Vector3.Lerp(transform.position, _correctPosition, Time.deltaTime * SmoothingDelay);
                transform.rotation = Quaternion.Lerp(transform.rotation, _correctRotation, Time.deltaTime * SmoothingDelay);
                if (_syncVelocity)
                    _mapObject.GameObject.rigidbody.velocity = _correctVelocity;
            }
        }
    }
}

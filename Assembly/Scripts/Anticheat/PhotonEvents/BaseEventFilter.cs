using System;
using UnityEngine;
using Settings;
using Utility;
using System.Collections.Generic;

namespace Anticheat
{
    class BaseEventFilter
    {
        protected virtual RateLimit TotalRateLimit => new RateLimit(100, 1f);
        protected virtual bool AlwaysAllowMaster => true;
        protected PhotonPlayer _player;
        protected PhotonEventType _eventType;

        public BaseEventFilter(PhotonPlayer player, PhotonEventType eventType)
        {
            _player = player;
            _eventType = eventType;
        }

        public bool IsMasterOrLocal()
        {
            if (_player.isLocal)
                return true;
            if (AlwaysAllowMaster && _player.isMasterClient)
                return true;
            return false;
        }

        public virtual bool CheckEvent(object[] data)
        {
            if (!TotalRateLimit.Use(1))
            {
                AnticheatManager.KickPlayer(_player, reason: "sending too many " + _eventType.ToString() + " events");
                return false;
            }
            return true;
        }
    }
}

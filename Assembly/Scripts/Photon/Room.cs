




using ExitGames.Client.Photon;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Room : RoomInfo
{
    internal Room(string roomName, RoomOptions options) : base(roomName, null)
    {
        if (options == null)
        {
            options = new RoomOptions();
        }
        base.visibleField = options.isVisible;
        base.openField = options.isOpen;
        base.maxPlayersField = (byte) options.maxPlayers;
        base.autoCleanUpField = false;
        base.CacheProperties(options.customRoomProperties);
        this.propertiesListedInLobby = options.customRoomPropertiesForLobby;
    }

    public void SetCustomProperties(Hashtable propertiesToSet)
    {
        if (propertiesToSet != null)
        {
            base.customProperties.MergeStringKeys(propertiesToSet);
            base.customProperties.StripKeysWithNullValues();
            Hashtable gameProperties = propertiesToSet.StripToStringKeys();
            if (!PhotonNetwork.offlineMode)
            {
                PhotonNetwork.networkingPeer.OpSetCustomPropertiesOfRoom(gameProperties, true, 0);
            }
            object[] parameters = new object[] { propertiesToSet };
            NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnPhotonCustomRoomPropertiesChanged, parameters);
        }
    }

    public void SetPropertiesListedInLobby(string[] propsListedInLobby)
    {
        Hashtable gameProperties = new Hashtable();
        gameProperties[(byte) 250] = propsListedInLobby;
        PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(gameProperties, false, 0);
        this.propertiesListedInLobby = propsListedInLobby;
    }

    public override string ToString()
    {
        object[] args = new object[] { base.nameField, !base.visibleField ? "hidden" : "visible", !base.openField ? "closed" : "open", base.maxPlayersField, this.playerCount };
        return string.Format("Room: '{0}' {1},{2} {4}/{3} players.", args);
    }

    public string ToStringFull()
    {
        object[] args = new object[] { base.nameField, !base.visibleField ? "hidden" : "visible", !base.openField ? "closed" : "open", base.maxPlayersField, this.playerCount, base.customProperties.ToStringFull() };
        return string.Format("Room: '{0}' {1},{2} {4}/{3} players.\ncustomProps: {5}", args);
    }

    public bool autoCleanUp
    {
        get
        {
            return base.autoCleanUpField;
        }
    }

    public int maxPlayers
    {
        get
        {
            return base.maxPlayersField;
        }
        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                Debug.LogWarning("Can't set maxPlayers when not in that room.");
            }
            if (value > 0xff)
            {
                Debug.LogWarning("Can't set Room.maxPlayers to: " + value + ". Using max value: 255.");
                value = 0xff;
            }
            if ((value != base.maxPlayersField) && !PhotonNetwork.offlineMode)
            {
                Hashtable gameProperties = new Hashtable();
                gameProperties.Add((byte) 0xff, (byte) value);
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(gameProperties, true, 0);
            }
            base.maxPlayersField = (byte) value;
        }
    }

    public string name
    {
        get
        {
            return base.nameField;
        }
        internal set
        {
            base.nameField = value;
        }
    }

    public bool open
    {
        get
        {
            return base.openField;
        }
        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                Debug.LogWarning("Can't set open when not in that room.");
            }
            if ((value != base.openField) && !PhotonNetwork.offlineMode)
            {
                Hashtable gameProperties = new Hashtable();
                gameProperties.Add((byte) 0xfd, value);
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(gameProperties, true, 0);
            }
            base.openField = value;
        }
    }

    public int playerCount
    {
        get
        {
            if (PhotonNetwork.playerList != null)
            {
                return PhotonNetwork.playerList.Length;
            }
            return 0;
        }
    }

    public string[] propertiesListedInLobby { get; private set; }

    public bool visible
    {
        get
        {
            return base.visibleField;
        }
        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                Debug.LogWarning("Can't set visible when not in that room.");
            }
            if ((value != base.visibleField) && !PhotonNetwork.offlineMode)
            {
                Hashtable gameProperties = new Hashtable();
                gameProperties.Add((byte) 0xfe, value);
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(gameProperties, true, 0);
            }
            base.visibleField = value;
        }
    }
}


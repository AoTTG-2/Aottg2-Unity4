using System;
using System.Collections.Generic;

static class PhotonExtensions
{
    public static void SetCustomProperty(this PhotonPlayer player, string key, object value)
    {
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        properties.Add(key, value);
        player.SetCustomProperties(properties);
    }

    public static void SetCustomProperties(this PhotonPlayer player, Dictionary<string, object> dictionary)
    {
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        foreach (string key in dictionary.Keys)
        {
            object value = dictionary[key];
            if (player.GetCustomProperty(key) != value)
                properties.Add(key, value);
        }
        if (properties.Count > 0)
            player.SetCustomProperties(properties);
    }

    public static object GetCustomProperty(this PhotonPlayer player, string key)
    {
        if (player.customProperties.ContainsKey(key))
            return player.customProperties[key];
        return null;
    }

    public static int GetIntProperty(this PhotonPlayer player, string key, int defaultValue = 0)
    {
        object obj = player.GetCustomProperty(key);
        if (obj != null && obj is int)
            return (int)obj;
        return defaultValue;
    }

    public static float GetFloatProperty(this PhotonPlayer player, string key, float defaultValue = 0)
    {
        object obj = player.GetCustomProperty(key);
        if (obj != null && obj is float)
            return (float)obj;
        return defaultValue;
    }

    public static bool GetBoolProperty(this PhotonPlayer player, string key, bool defaultValue = false)
    {
        object obj = player.GetCustomProperty(key);
        if (obj != null && obj is bool)
            return (bool)obj;
        return defaultValue;
    }

    public static string GetStringProperty(this PhotonPlayer player, string key, string defaultValue = "")
    {
        object obj = player.GetCustomProperty(key);
        if (obj != null && obj is string)
            return (string)obj;
        return defaultValue;
    }

    public static string GetStringProperty(this RoomInfo room, string key, string defaultValue = "")
    {
        if (room.customProperties.ContainsKey(key))
        {
            object obj = room.customProperties[key];
            if (obj != null && obj is string)
                return (string)obj;
        }
        return defaultValue;
    }
}

using ExitGames.Client.Photon;
using UnityEngine;

internal static class CustomTypes
{
    public const byte PhotonPlayerCode = 80;
    public const short PhotonPlayerLength = 4;
    public const byte QuaternionCode = 81;
    public const short QuaternionLength = 16;
    public const byte Vector2Code = 87;
    public const short Vector2Length = 8;
    public const byte Vector3Code = 86;
    public const short Vector3Length = 12;

    private static object DeserializePhotonPlayer(StreamBuffer buff, short length)
    {
        int ID;
        int index;
        byte[] buffer = buff.GetBufferAndAdvance(length, out index);
        Protocol.Deserialize(out ID, buffer, ref index);
        PhotonPlayer player = PhotonPlayer.Find(ID);
        if (player == null)
        {
            return PhotonNetwork.player;
        }
        return player;
    }

    private static object DeserializeQuaternion(StreamBuffer buff, short length)
    {
        Quaternion quaternion = default(Quaternion);
        int num;
        byte[] bytes = buff.GetBufferAndAdvance(length, out num);
        Protocol.Deserialize(out quaternion.w, bytes, ref num);
        Protocol.Deserialize(out quaternion.x, bytes, ref num);
        Protocol.Deserialize(out quaternion.y, bytes, ref num);
        Protocol.Deserialize(out quaternion.z, bytes, ref num);
        return quaternion;
    }

    private static object DeserializeVector2(StreamBuffer buff, short length)
    {
        Vector2 vector = default(Vector2);
        int num;
        byte[] bytes = buff.GetBufferAndAdvance(length, out num);
        Protocol.Deserialize(out vector.x, bytes, ref num);
        Protocol.Deserialize(out vector.y, bytes, ref num);
        return vector;
    }

    private static object DeserializeVector3(StreamBuffer buff, short length)
    {
        Vector3 vector = default(Vector3);
        int num;
        byte[] bytes = buff.GetBufferAndAdvance(length, out num);
        Protocol.Deserialize(out vector.x, bytes, ref num);
        Protocol.Deserialize(out vector.y, bytes, ref num);
        Protocol.Deserialize(out vector.z, bytes, ref num);
        return vector;
    }

    private static short SerializePhotonPlayer(StreamBuffer buff, object customobject)
    {
        int id = ((PhotonPlayer)customobject).ID;
        int num;
        byte[] array = buff.GetBufferAndAdvance(PhotonPlayerLength, out num);
        Protocol.Serialize(id, array, ref num);
        return PhotonPlayerLength;
    }

    private static short SerializeQuaternion(StreamBuffer buff, object obj)
    {
        Quaternion quaternion = (Quaternion)obj;
        int num;
        byte[] array = buff.GetBufferAndAdvance(QuaternionLength, out num);
        Protocol.Serialize(quaternion.w, array, ref num);
        Protocol.Serialize(quaternion.x, array, ref num);
        Protocol.Serialize(quaternion.y, array, ref num);
        Protocol.Serialize(quaternion.z, array, ref num);
        return QuaternionLength;
    }

    private static short SerializeVector2(StreamBuffer buff, object customobject)
    {
        Vector2 vector = (Vector2)customobject;
        int num;
        byte[] array = buff.GetBufferAndAdvance(Vector2Length, out num);
        Protocol.Serialize(vector.x, array, ref num);
        Protocol.Serialize(vector.y, array, ref num);
        return Vector2Length;
    }

    private static short SerializeVector3(StreamBuffer buff, object customobject)
    {
        Vector3 vector = (Vector3)customobject;
        int num;
        byte[] array = buff.GetBufferAndAdvance(Vector3Length, out num);
        Protocol.Serialize(vector.x, array, ref num);
        Protocol.Serialize(vector.y, array, ref num);
        Protocol.Serialize(vector.z, array, ref num);
        return Vector3Length;
    }

    public static void Register()
    {
        PhotonPeer.RegisterType(typeof(Quaternion), QuaternionCode, new SerializeStreamMethod(SerializeQuaternion), new DeserializeStreamMethod(DeserializeQuaternion));
        PhotonPeer.RegisterType(typeof(Vector2), Vector2Code, new SerializeStreamMethod(SerializeVector2), new DeserializeStreamMethod(DeserializeVector2));
        PhotonPeer.RegisterType(typeof(Vector3), Vector3Code, new SerializeStreamMethod(SerializeVector3), new DeserializeStreamMethod(DeserializeVector3));
        PhotonPeer.RegisterType(typeof(PhotonPlayer), PhotonPlayerCode, new SerializeStreamMethod(SerializePhotonPlayer), new DeserializeStreamMethod(DeserializePhotonPlayer));
    }
}
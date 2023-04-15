using System;
using System.Reflection;
using UnityEngine;
using Photon;

public class PhotonView: Photon.MonoBehaviour
{
    protected internal bool destroyedByPhotonNetworkOrQuit;
    protected internal bool didAwake;
    private bool failedToFindOnSerialize;
    public int group;
    private object[] instantiationDataField;
    public int instantiationId;
    protected internal object[] lastOnSerializeDataReceived;
    protected internal object[] lastOnSerializeDataSent;
    protected internal bool mixedModeIsReliable;
    public Component observed;
    private MethodInfo OnSerializeMethodInfo;
    public OnSerializeRigidBody onSerializeRigidBodyOption = OnSerializeRigidBody.All;
    public OnSerializeTransform onSerializeTransformOption = OnSerializeTransform.PositionAndRotation;
    public int ownerId;
    public int prefixBackup = -1;
    public int subId;
    public ViewSynchronization synchronization;
}

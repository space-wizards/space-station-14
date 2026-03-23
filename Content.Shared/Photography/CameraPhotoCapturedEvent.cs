using Robust.Shared.Serialization;

namespace Content.Shared.Photography;

[Serializable, NetSerializable]
public sealed class CameraPhotoCapturedEvent : HandledEntityEventArgs
{
    public NetEntity CameraNetUid;
    public byte[] PhotoBytes;
    public float FontSize;

    public CameraPhotoCapturedEvent(NetEntity cameraUid, byte[] photoBytes)
    {
        CameraNetUid = cameraUid;
        PhotoBytes = photoBytes;
    }
}

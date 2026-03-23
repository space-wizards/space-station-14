using Robust.Shared.Serialization;

namespace Content.Shared.Photography;

[Serializable, NetSerializable]
public sealed class CameraPhotoCapturedEvent : HandledEntityEventArgs
{
    public NetEntity CameraNetUid;
    public string GeneratedText;

    public CameraPhotoCapturedEvent(NetEntity cameraUid, string text)
    {
        CameraNetUid = cameraUid;
        GeneratedText = text;
    }
}

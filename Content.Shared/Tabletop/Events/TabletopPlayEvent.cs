using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events;

/// <summary>
/// An event sent by the server to the client to tell the client to open a tabletop game window.
/// </summary>
[Serializable, NetSerializable]
public sealed class TabletopPlayEvent(NetEntity tableUid, NetEntity cameraUid, string title, Vector2i size)
    : EntityEventArgs
{
    public NetEntity TableUid = tableUid;
    public NetEntity CameraUid = cameraUid;
    public string Title = title;
    public Vector2i Size = size;
}

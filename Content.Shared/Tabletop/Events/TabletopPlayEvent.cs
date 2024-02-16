using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    /// <summary>
    /// An event sent by the server to the client to tell the client to open a tabletop game window.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class TabletopPlayEvent : EntityEventArgs
    {
        public NetEntity TableUid;
        public NetEntity CameraUid;
        public string Title;
        public Vector2i Size;

        public TabletopPlayEvent(NetEntity tableUid, NetEntity cameraUid, string title, Vector2i size)
        {
            TableUid = tableUid;
            CameraUid = cameraUid;
            Title = title;
            Size = size;
        }
    }
}

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
        public Vector2i WindowSize; //SS220-initial-tabletop-window-size

        public TabletopPlayEvent(NetEntity tableUid, NetEntity cameraUid, string title, Vector2i size, Vector2i windowSize)
        {
            TableUid = tableUid;
            CameraUid = cameraUid;
            Title = title;
            Size = size;
            WindowSize = windowSize;
        }
    }
}

using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Farming
{
    [Serializable, NetSerializable]
    public sealed partial class PloughDoAfterEvent : DoAfterEvent
    {
        public NetEntity GridUid { get; }
        public Vector2i SnapPos { get; }

        public PloughDoAfterEvent(NetEntity gridUid, Vector2i snapPos)
        {
            GridUid = gridUid;
            SnapPos = snapPos;
        }

        public override DoAfterEvent Clone() => this;
    }
}
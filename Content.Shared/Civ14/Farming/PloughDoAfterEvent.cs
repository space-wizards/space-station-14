using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Farming
{
    [Serializable, NetSerializable]
    public sealed partial class PloughDoAfterEvent : DoAfterEvent
    {
        public NetEntity GridUid { get; }
        public Vector2i SnapPos { get; }
        public PloughActionType ActionType { get; } // Novo campo para indicar o tipo de ação

        public PloughDoAfterEvent(NetEntity gridUid, Vector2i snapPos, PloughActionType actionType)
        {
            GridUid = gridUid;
            SnapPos = snapPos;
            ActionType = actionType;
        }

        public override DoAfterEvent Clone() => this;
    }
}
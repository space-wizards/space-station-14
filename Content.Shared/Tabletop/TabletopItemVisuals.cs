using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    [Serializable, NetSerializable]
    public enum TabletopItemVisuals : byte
    {
        Scale,
        DrawDepth
    }
}

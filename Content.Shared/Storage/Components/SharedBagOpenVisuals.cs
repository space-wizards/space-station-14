using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components
{
    [Serializable, NetSerializable]
    public enum SharedBagOpenVisuals : byte
    {
        BagState,
    }

    [Serializable, NetSerializable]
    public enum SharedBagState : byte
    {
        Open,
        Closed,
    }
}

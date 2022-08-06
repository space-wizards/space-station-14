using Robust.Shared.Serialization;

namespace Content.Shared.Cabinet
{
    [Serializable, NetSerializable]
    public enum ItemCabinetVisuals : byte
    {
        IsOpen,
        ContainsItem
    }
}

using Robust.Shared.Serialization;

namespace Content.Shared.Electrocution;

[Serializable, NetSerializable]
public enum ElectrifiedLayers : byte
{
    Powered
}

[Serializable, NetSerializable]
public enum ElectrifiedVisuals : byte
{
    IsPowered
}

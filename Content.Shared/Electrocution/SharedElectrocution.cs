using Robust.Shared.Serialization;

namespace Content.Shared.Electrocution;

[Serializable, NetSerializable]
public enum ElectrifiedLayers : byte
{
    Electrified
}

[Serializable, NetSerializable]
public enum ElectrifiedVisuals : byte
{
    IsActive
}

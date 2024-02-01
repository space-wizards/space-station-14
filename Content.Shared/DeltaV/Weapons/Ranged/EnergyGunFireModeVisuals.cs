using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Weapons.Ranged;

[Serializable, NetSerializable]
public enum EnergyGunFireModeVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum EnergyGunFireModeState : byte
{
    Disabler,
    Lethal,
    Special
}

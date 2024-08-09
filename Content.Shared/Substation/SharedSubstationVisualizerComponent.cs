using Robust.Shared.Serialization;

namespace Content.Shared.Substation;

[Serializable, NetSerializable]
public enum SubstationVisuals
{
    LastChargeState,
}

// enum use copied from Apc code to use same state names as substation sprites
[Serializable, NetSerializable]
public enum SubstationChargeState : sbyte
{
    Dead = 0,
    Discharging = 1,
    Charging = 2,
    Full = 3
}
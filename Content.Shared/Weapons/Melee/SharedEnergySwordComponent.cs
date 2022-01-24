using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee;


[Serializable, NetSerializable, Flags]
public enum EnergySwordStatus : byte
{
    Off = 0,
    On = 1 << 0,
    Hacked = 1 << 1,
}

[Serializable, NetSerializable]
public enum EnergySwordVisuals : byte
{
    State,
    Color,
}

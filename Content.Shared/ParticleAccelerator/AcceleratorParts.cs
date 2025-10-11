using Robust.Shared.Serialization;

namespace Content.Shared.ParticleAccelerator;

[Serializable, NetSerializable]
public enum AcceleratorParts : byte
{
    EndCap,
    FuelChamber,
    PowerBox,
    PortEmitter,
    ForeEmitter,
    StarboardEmitter
};



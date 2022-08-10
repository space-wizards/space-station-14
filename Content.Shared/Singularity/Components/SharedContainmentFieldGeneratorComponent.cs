using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components;
public abstract class SharedContainmentFieldGeneratorComponent : Component { }

[Serializable, NetSerializable]
public enum ContainmentFieldGeneratorVisuals : byte
{
    PowerLight,
    FieldLight,
    OnLight,
}

[Serializable, NetSerializable]
public enum PowerLevelVisuals : byte
{
    NoPower,
    LowPower,
    MediumPower,
    HighPower,
}

[Serializable, NetSerializable]
public enum FieldLevelVisuals : byte
{
    NoLevel,
    On,
    OneField,
    MultipleFields,
}

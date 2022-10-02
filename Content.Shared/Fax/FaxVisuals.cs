using Robust.Shared.Serialization;

namespace Content.Shared.Fax;

[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    BaseState,
}

[Serializable, NetSerializable]
public enum FaxMachineVisualState : byte
{
    Normal,
    Inserting,
    Printing
}

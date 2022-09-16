using Robust.Shared.Serialization;

namespace Content.Shared.Fax;

[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    IsSending,
    IsReceiving
}
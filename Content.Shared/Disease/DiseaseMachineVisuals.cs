using Robust.Shared.Serialization;

namespace Content.Shared.Disease
{
    [Serializable, NetSerializable]
    public enum DiseaseMachineVisuals : byte
    {
        IsOn,
        IsRunning
    }
}

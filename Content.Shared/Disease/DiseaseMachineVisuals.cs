using Robust.Shared.Serialization;

namespace Content.Shared.Disease
{
    /// <summary>
    /// Stores bools for if the machine is on
    /// and if it's currently running.
    /// Used for the visualizer
    /// </summary>
    [Serializable, NetSerializable]
    public enum DiseaseMachineVisuals : byte
    {
        IsOn,
        IsRunning
    }
}

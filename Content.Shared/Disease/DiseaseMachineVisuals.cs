using Robust.Shared.Serialization;

namespace Content.Shared.Disease
{
    [Serializable, NetSerializable]
    /// <summary>
    /// Stores bools for if the machine is on
    /// and if it's currently running.
    /// Used for the visualizer
    /// </summary>
    public enum DiseaseMachineVisuals : byte
    {
        IsOn,
        IsRunning
    }
}

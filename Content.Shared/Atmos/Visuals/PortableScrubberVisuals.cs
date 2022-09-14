using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Visuals
{
    [Serializable, NetSerializable]
    /// <summary>
    /// Used for the visualizer
    /// </summary>
    public enum PortableScrubberVisuals : byte
    {
        IsFull,
        IsRunning,
        IsDraining
    }
}

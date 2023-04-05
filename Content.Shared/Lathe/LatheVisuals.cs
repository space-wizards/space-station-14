using Robust.Shared.Serialization;

namespace Content.Shared.Lathe
{
    /// <summary>
    /// Stores bools for if the machine is on
    /// and if it's currently running and/or inserting.
    /// Used for the visualizer
    /// </summary>
    [Serializable, NetSerializable]
    public enum LatheVisuals : byte
    {
        IsRunning,
        IsInserting,
        InsertingColor
    }
}

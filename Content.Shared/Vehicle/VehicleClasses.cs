using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.Vehicle
{
    [Serializable, NetSerializable]
    /// <summary>
    /// Stores the vehicle's draw depth mostly
    /// </summary>
    public enum VehicleVisuals : byte
    {
        DrawDepth,
        AutoAnimate,
        StorageUsed
    }

    public sealed class HonkActionEvent : PerformActionEvent { }
}

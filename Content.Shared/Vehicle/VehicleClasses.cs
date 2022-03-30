using Robust.Shared.Serialization;
using Content.Shared.Actions;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
namespace Content.Shared.Vehicle
{
    /// <summary>
    /// Stores the vehicle's draw depth mostly
    /// </summary>
    [Serializable, NetSerializable]
    public enum VehicleVisuals : byte
    {
        /// <summary>
        /// What layer the vehicle should draw on (assumed integer)
        /// </summary>
        DrawDepth,
        /// <summary>
        /// Whether the wheels should be turning
        /// </summary>
        AutoAnimate,
        /// <summary>
        /// Whether the trash bag or similar should be visible
        /// </summary>
        StorageUsed
    }
    /// <summary>
    /// Raised when someone honks a vehicle horn
    /// </summary>
    public sealed class HonkActionEvent : PerformActionEvent { }
}

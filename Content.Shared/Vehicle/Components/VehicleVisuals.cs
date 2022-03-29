using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle
{
    [Serializable, NetSerializable]
    /// <summary>
    /// Stores the vehicle's draw depth mostly
    /// </summary>
    public enum VehicleVisuals : byte
    {
        DrawDepth
    }
}

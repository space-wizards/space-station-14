using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// This is used for controlling evacuation for a station.
/// </summary>
[RegisterComponent]
public sealed partial class StationEmergencyShuttleComponent : Component
{
    /// <summary>
    /// The emergency shuttle assigned to this station.
    /// </summary>
    [DataField, Access(typeof(ShuttleSystem), typeof(EmergencyShuttleSystem), Friend = AccessPermissions.ReadWrite)]
    public EntityUid? EmergencyShuttle;

    /// <summary>
    /// Emergency shuttle map path for this station.
    /// </summary>
    [DataField("emergencyShuttlePath", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath EmergencyShuttlePath { get; set; } = new("/Maps/Shuttles/emergency.yml");
}

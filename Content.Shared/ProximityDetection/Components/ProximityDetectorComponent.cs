using Content.Shared.ProximityDetection.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ProximityDetection.Components;

/// <summary>
/// Used to search for the closest entity with a range that matches specified requirements (tags and/or components).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(ProximityDetectionSystem))]
public sealed partial class ProximityDetectorComponent : Component
{
    /// <summary>
    /// The criteria used to filter entities.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Criteria = new();

    /// <summary>
    /// The entity that was found.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// The distance to <see cref="Target"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Distance = -1;

    /// <summary>
    /// The farthest distance to search for targets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 10f;

    /// <summary>
    /// How often detector updates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time detector updates.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

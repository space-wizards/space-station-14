using Content.Shared.FixedPoint;
using Content.Shared.ProximityDetection.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ProximityDetection.Components;
/// <summary>
/// This is used to search for the closest entity with a range that matches specified requirements (tags and/or components)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState ,Access(typeof(ProximityDetectionSystem))]
public sealed partial class ProximityDetectorComponent : Component
{
    /// <summary>
    /// The criteria used to filter entities
    /// Note: RequireAll is only supported for tags, all components are required to count as a match!
    /// </summary>
    [DataField( required: true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist Criteria = new();

    /// <summary>
    /// Found Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? TargetEnt;

    /// <summary>
    /// Distance to Found Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Distance = -1;

    /// <summary>
    /// The farthest distance to search for targets
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Range = 10f;

    // TODO: use timespans not this
    public float AccumulatedFrameTime;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float UpdateRate = 0.3f;
}

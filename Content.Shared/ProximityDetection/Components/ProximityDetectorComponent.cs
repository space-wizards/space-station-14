using Content.Shared.ProximityDetection.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ProximityDetection.Components;
/// <summary>
/// This is used for an item that beeps based on
/// proximity to a specified component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState ,Access(typeof(ProximityDetectorSystem))]
public sealed partial class ProximityDetectorComponent : Component
{
    /// <summary>
    /// Whether or not it's on.
    /// </summary>
    [DataField("enabled"), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    [DataField("targetRequirements", required: true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist TargetRequirements = default!;

    /// <summary>
    /// The farthest distance to search for targets
    /// </summary>
    [DataField("maximumDistance"), ViewVariables(VVAccess.ReadWrite)]
    public float MaximumDistance = 10f;

    public float AccumulatedFrameTime;

    [DataField("updateRate"), ViewVariables(VVAccess.ReadWrite)]
    public float UpdateRate = 1;
}

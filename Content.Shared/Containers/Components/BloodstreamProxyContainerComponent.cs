using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers.Components;

/// <summary>
/// Component for containers that proxy bloodstream access to a contained victim.
/// When an entity with this component is targeted for injection, the injection
/// will be redirected to the victim contained within.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodstreamProxyContainerComponent : Component
{
    /// <summary>
    /// The container ID that holds the victim entity.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId = string.Empty;
}


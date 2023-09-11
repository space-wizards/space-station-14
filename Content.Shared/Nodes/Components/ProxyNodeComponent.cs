using Content.Shared.Nodes.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nodes.Components;

[Access(typeof(SharedNodeGraphSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProxyNodeComponent : Component
{
    /// <summary>
    /// The uid of the poly node/node container that this node is acting as a proxy for.
    /// </summary>
    [DataField("proxyFor")]
    public EntityUid? ProxyFor = default!;

    /// <summary>
    /// The key this node is being indexed under in its node container.
    /// </summary>
    [DataField("proxyKey")]
    public string ProxyKey = default!;
}

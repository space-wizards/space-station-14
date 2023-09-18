namespace Content.Server.Nodes.Components;

/// <summary>
/// Component used to mark nodes as acting as proxies for some other entity.
/// Usually used when some entity wants to be a member of multiple graphs at once.
/// </summary>
[RegisterComponent]
public sealed partial class ProxyNodeComponent : Component
{
    /// <summary>
    /// The uid of the poly node/node container that this node is acting as a proxy for.
    /// </summary>
    [ViewVariables]
    public EntityUid? ProxyFor = null;

    /// <summary>
    /// The key this node is being indexed under in its node container.
    /// </summary>
    [ViewVariables]
    public string? ProxyKey = null;
}

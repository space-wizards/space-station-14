namespace Content.Server.Nodes.Components;

/// <summary>
/// </summary>
[RegisterComponent]
public sealed partial class PolyNodeComponent : Component
{
    /// <summary>
    /// A map of node prototypes to use when generating proxy nodes for this node container.
    /// </summary>
    [DataField("proxies")]
    public Dictionary<string, string>? ProxyPrototypes = new();

    /// <summary>
    /// The key to use when mapping ourself as a proxy.
    /// </summary>
    [DataField("proxySelf")]
    public string? ProxySelf = null;

    /// <summary>
    /// A map of the nodes acting as proxies for this node container.
    /// </summary>
    [DataField("proxyNodes")]
    public Dictionary<string, EntityUid> ProxyNodes = new();
}

using Content.Shared.Nodes.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nodes.Components;

[Access(typeof(SharedNodeGraphSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PolyNodeComponent : Component
{
    /// <summary>
    /// </summary>
    [AutoNetworkedField]
    [DataField("proxyNodes")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, EntityUid> ProxyNodes { get; init; } = new();

    /// <summary>
    /// </summary>
    [DataField("proxies")]
    [ViewVariables]
    public List<(string Id, string Proto)>? ProxyPrototypes { get; init; } = new();

    /// <summary>
    /// </summary>
    [DataField("proxySelf")]
    [ViewVariables]
    public string? ProxySelf = null;
}

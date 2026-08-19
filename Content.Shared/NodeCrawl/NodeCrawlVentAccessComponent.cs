using Robust.Shared.GameStates;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Marker component to use on atmos-related devices that node crawlers can enter/exit out of.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NodeCrawlVentAccessComponent : Component
{
    public const string ComponentName = "NodeCrawlVentAccess";
}

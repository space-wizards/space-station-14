using Robust.Shared.GameStates;

namespace Content.Shared.VentCrawl.Components;

/// <summary>
/// Marks a traversal holder as using vent-crawl gas-pipe behavior.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class VentCrawlHolderComponent : Component
{
    /// <summary>
    /// Minimum delay between manifold layer selections.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan ManifoldLayerSelectionCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Timestamp of the last manifold layer selection.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan ManifoldLastLayerSelection;
}

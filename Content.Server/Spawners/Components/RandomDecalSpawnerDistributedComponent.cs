using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class RandomDecalSpawnerDistributedComponent : RandomDecalSpawnerComponent
{
    /// <summary>
    /// Whether decals should snap to the center of a grid space or be placed randomly within them.
    /// </summary>
    [DataField]
    public bool SnapPosition = false;

    /// <summary>
    /// Per grid space, the maximum amount of decals that can spawn on any particular grid space.
    /// </summary>
    [DataField]
    public int MaxDecalsPerTile = 1;
}

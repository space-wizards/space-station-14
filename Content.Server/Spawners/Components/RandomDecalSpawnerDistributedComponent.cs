namespace Content.Server.Spawners.Components;

public sealed partial class RandomDecalSpawnerDistributedComponent : RandomDecalSpawnerComponent
{
    /// <summary>
    /// Per grid space, the maximum amount of decals that can spawn on any particular grid space.
    /// </summary>
    [DataField]
    public int MaxDecalsPerTile = 1;
}

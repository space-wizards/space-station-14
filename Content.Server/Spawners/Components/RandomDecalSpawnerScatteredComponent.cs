namespace Content.Server.Spawners.Components;

public sealed partial class RandomDecalSpawnerScatteredComponent : RandomDecalSpawnerComponent
{
    /// <summary>
    /// The maximum amount of decals to spawn across the entire radius.
    /// </summary>
    [DataField]
    public int MaxDecals = 1;
}

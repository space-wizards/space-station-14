using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class RandomDecalSpawnerScatteredComponent : RandomDecalSpawnerComponent
{
    /// <summary>
    /// The maximum amount of decals to spawn across the entire radius.
    /// </summary>
    [DataField]
    public int MaxDecals = 1;
}

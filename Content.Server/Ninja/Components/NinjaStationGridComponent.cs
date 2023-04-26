using Content.Server.Ninja.Systems;

namespace Content.Server.Ninja.Components;

/// <summary>
/// Used by space ninja spawner to indicate what station grid to head towards, and the ninja rule config.
/// </summary>
[RegisterComponent, Access(typeof(NinjaSystem))]
public sealed class NinjaSpawnerDataComponent : Component
{
    /// <summary>
    /// The grid uid being targeted.
    /// </summary>
    public EntityUid Grid;

    /// <summary>
    /// The rule entity that spawned this ninja.
    /// </summary>
    public EntityUid Rule;
}

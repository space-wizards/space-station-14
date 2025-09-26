using Content.Server.Objectives.Systems;
using Content.Shared.Mind.Filters;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person from a pool and filters.
/// </summary>
/// <remarks>
/// Don't copy paste this for a new objective, if you need a new filter just make a new filter and set it in YAML.
/// </remarks>
[RegisterComponent, Access(typeof(PickObjectiveTargetSystem))]
public sealed partial class PickRandomPersonComponent : Component
{
    /// <summary>
    /// A pool to pick potential targets from.
    /// </summary>
    [DataField]
    public IMindPool Pool = new AliveHumansPool();

    /// <summary>
    /// Filters to apply to <see cref="Pool"/>.
    /// </summary>
    [DataField]
    public List<MindFilter> Filters = new();
}

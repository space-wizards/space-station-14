using Content.Server.Objectives.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Objectives.Systems;

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
    /// EntityConditions to apply to <see cref="Pool"/>.
    /// If these conditions pass the mind is valid.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public EntityCondition[] Conditions;
}

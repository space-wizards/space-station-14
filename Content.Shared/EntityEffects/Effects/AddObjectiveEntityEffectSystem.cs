using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds an objective to this entity's mind.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddObjectiveEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddObjective>
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddObjective> args)
    {
        if (_net.IsClient || !_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        _mind.TryAddObjective(mindId, mind, args.Effect.Objective);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddObjective : EntityEffectBase<AddObjective>
{
    /// <summary>
    /// Objective prototype to assign.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ObjectiveComponent> Objective;
}

using Content.Shared.EntityEffects;

namespace Content.Shared.ActionSequence;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class EntityEffectActionStepSystem : ActionStepSystem<EntityEffectActionStep>
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<ActionSequenceComponent> entity, ref ActionStepEvent<EntityEffectActionStep> args)
    {
        Log.Debug("Entity effect detected");
        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.UserKey, out var userKey) || userKey is not EntityUid user)
            return;

        if (!entity.Comp.Blackboard.TryGetValue(args.Effect.TargetKey, out var targetKey) || targetKey is not EntityUid target)
            return;

        _effects.ApplyEffects(target, args.Effect.Effects.ToArray(), user: user);
    }
}

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class EntityEffectActionStep : ActionStepBase<EntityEffectActionStep>
{
    [DataField(required: true)]
    public List<EntityEffect> Effects = [];
}

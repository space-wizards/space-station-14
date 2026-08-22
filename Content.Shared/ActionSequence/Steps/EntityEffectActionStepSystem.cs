using Content.Shared.EntityEffects;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="EntityEffectActionStep"/>.
/// </summary>
public sealed partial class EntityEffectActionStepSystem : ActionStepSystem<EntityEffectActionStep>
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<EntityEffectActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.UserKey, out var user))
            return;

        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            return;

        _effects.ApplyEffects(target, args.Step.Effects.ToArray(), user: user);
    }
}

/// <summary>
/// Applies a list of entity effects to the TargetKey as the UserKey.
/// </summary>
public sealed partial class EntityEffectActionStep : ActionStepBase<EntityEffectActionStep>
{
    [DataField(required: true)]
    public List<EntityEffect> Effects = [];
}

using Content.Shared.Changeling.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Changeling.Systems;

/// <summary>
/// Handles the logic for <see cref="HumanoidTransformStatusEffectComponent"/> status effects.
/// Uses <see cref="ChangelingTransformSystem"/> to apply and revert transformations.
/// </summary>
public sealed partial class HumanoidTransformStatusEffectSystem : EntitySystem
{
    [Dependency] private ChangelingTransformSystem _changelingTransform = default!;
    [Dependency] private SharedChangelingIdentitySystem _changelingIdentity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidTransformStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<HumanoidTransformStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    /// <summary>
    /// Ensures forced transformation component exists on the effect, updates its target identity and
    /// reapplies the disguise if the effect is already active.
    /// </summary>
    public void RefreshHumanoidTransform(EntityUid target, EntityUid effect, EntityUid targetIdentity)
    {
        if (!TryComp<HumanoidTransformStatusEffectComponent>(effect, out var effectComp))
            return;

        effectComp.TargetIdentity = targetIdentity;
        Dirty(effect, effectComp);

        _changelingTransform.TryApplyIdentity(target, targetIdentity, effectComp.CloningSettings);
    }

    private void OnApplied(Entity<HumanoidTransformStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.OriginalIdentity = _changelingIdentity.CloneToPausedMap(ent.Comp.CloningSettings, args.Target);
        Dirty(ent, ent.Comp);

        if (ent.Comp.OriginalIdentity == null)
            return;

        if (ent.Comp.TargetIdentity == null || !Exists(ent.Comp.TargetIdentity.Value))
            return;

        _changelingTransform.TryApplyIdentity(args.Target, ent.Comp.TargetIdentity.Value, ent.Comp.CloningSettings);
    }

    private void OnRemoved(Entity<HumanoidTransformStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.OriginalIdentity == null || !Exists(ent.Comp.OriginalIdentity.Value))
            return;

        // Don't apply identity if the target is being deleted to avoid exceptions.
        if (TerminatingOrDeleted(args.Target) || EntityManager.IsQueuedForDeletion(args.Target))
            return;

        _changelingTransform.TryApplyIdentity(args.Target, ent.Comp.OriginalIdentity.Value, ent.Comp.CloningSettings);

        // Cleanup the snapshot.
        QueueDel(ent.Comp.OriginalIdentity.Value);
    }
}

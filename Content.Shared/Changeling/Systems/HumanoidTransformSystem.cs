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

    [SubscribeLocalEvent]
    private void OnApplied(Entity<HumanoidTransformStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.OriginalIdentity = _changelingIdentity.CloneToPausedMap(ent.Comp.CloningSettings, args.Target);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<HumanoidTransformStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.OriginalIdentity is not { } originalIdentity)
            return;

        // Don't apply identity if the target is being deleted to avoid exceptions.
        if (!TerminatingOrDeleted(args.Target) && !EntityManager.IsQueuedForDeletion(args.Target))
            _changelingTransform.TryApplyIdentity(args.Target, originalIdentity, ent.Comp.CloningSettings);

        QueueDel(originalIdentity);
    }

    /// <summary>
    /// Applies or replaces the disguise without overwriting the original identity snapshot.
    /// </summary>
    public bool RefreshHumanoidTransform(EntityUid target, EntityUid effect, EntityUid targetIdentity)
    {
        if (!TryComp<HumanoidTransformStatusEffectComponent>(effect, out var effectComp) ||
            effectComp.OriginalIdentity == null)
            return false;

        return _changelingTransform.TryApplyIdentity(target, targetIdentity, effectComp.CloningSettings);
    }
}

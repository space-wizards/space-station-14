using Content.Shared.Body.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;
using Content.Shared.Traits.Assorted;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainComponent, OrganGotInsertedEvent>((uid, _, args) => HandleMind(args.Target, uid));
        SubscribeLocalEvent<BrainComponent, OrganGotRemovedEvent>((uid, _, args) => HandleMind(uid, args.Target));
        SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
    }

    private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
    {
        if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
            return;

        var newMindCont = EnsureComp<MindContainerComponent>(newEntity);
        var oldMindCont = EnsureComp<MindContainerComponent>(oldEntity);

        // A mind being moved from body -> brain counts as having inhabited the same container, even if the mind has since left.
        if (HasComp<BrainComponent>(newEntity) && oldMindCont.LastMind != null)
            _mindSystem.SetLastMind((newEntity, newMindCont), oldMindCont.LastMind);

        var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
        ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

        if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind) || HasComp<MindUntransferableToBrainComponent>(oldEntity))
            return;

        _mindSystem.TransferTo(mindId, newEntity, mind: mind);
    }

    private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }
}

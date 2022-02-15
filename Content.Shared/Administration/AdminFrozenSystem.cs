using Content.Shared.ActionBlocker;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Throwing;

namespace Content.Shared.Administration;

public sealed class AdminFrozenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AdminFrozenComponent, UseAttemptEvent>(CheckAct);
        SubscribeLocalEvent<AdminFrozenComponent, PickupAttemptEvent>(CheckAct);
        SubscribeLocalEvent<AdminFrozenComponent, ThrowAttemptEvent>(CheckAct);
        SubscribeLocalEvent<AdminFrozenComponent, AttackAttemptEvent>(CheckAct);
        SubscribeLocalEvent<AdminFrozenComponent, MovementAttemptEvent>(CheckAct);
        SubscribeLocalEvent<AdminFrozenComponent, InteractionAttemptEvent>(CheckAct);

        SubscribeLocalEvent<AdminFrozenComponent, ComponentInit>(OnFreezeInit);
        SubscribeLocalEvent<AdminFrozenComponent, ComponentShutdown>(OnFreezeShutdown);
    }

    private void CheckAct(EntityUid uid, AdminFrozenComponent component, CancellableEntityEventArgs args)
    {
        if (component.LifeStage > ComponentLifeStage.Initialized) return;
        args.Cancel();
    }

    private void OnFreezeInit(EntityUid uid, AdminFrozenComponent component, ComponentInit args)
    {
        _blocker.RefreshCanMove(uid);
    }

    private void OnFreezeShutdown(EntityUid uid, AdminFrozenComponent component, ComponentShutdown args)
    {
        _blocker.RefreshCanMove(uid);
    }
}

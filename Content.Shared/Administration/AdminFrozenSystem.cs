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
        base.Initialize();

        SubscribeLocalEvent<AdminFrozenComponent, UseAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, PickupAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, ThrowAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, InteractionAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<AdminFrozenComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<AdminFrozenComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    private void OnUpdateCanMove(EntityUid uid, AdminFrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void UpdateCanMove(EntityUid uid, AdminFrozenComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }
}

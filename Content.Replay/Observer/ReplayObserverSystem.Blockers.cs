using Content.Shared.DragDrop;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Physics.Pull;
using Content.Shared.Throwing;

namespace Content.Replay.Observer;

public sealed partial class ReplayObserverSystem : EntitySystem
{
    private void InitializeBlockers()
    {
        // Block most interactions to avoid mispredicts

        SubscribeLocalEvent<ReplayObserverComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, InteractionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, IsEquippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, IsUnequippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ReplayObserverComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ReplayObserverComponent, ChangeDirectionAttemptEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ReplayObserverComponent, PullAttemptEvent>(OnPullAttempt);
    }

    private void OnAttempt(EntityUid uid, ReplayObserverComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnUpdateCanMove(EntityUid uid, ReplayObserverComponent component, CancellableEntityEventArgs args)
    {
        if (!uid.IsClientSide())
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, ReplayObserverComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }
}

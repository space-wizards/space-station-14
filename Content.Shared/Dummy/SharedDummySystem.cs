using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Emoting;
using Content.Shared.Movement.Events;

namespace Content.Shared.Dummy
{
    public abstract class SharedDummySystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DummyComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<DummyComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<DummyComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<DummyComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<DummyComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<DummyComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<DummyComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<DummyComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, DummyComponent component, ComponentStartup args)
        {
            _blocker.UpdateCanMove(uid);
        }

        private void OnMoveAttempt(EntityUid uid, DummyComponent component, UpdateCanMoveEvent args)
        {
            if (component.LifeStage > ComponentLifeStage.Running)
                return;

            args.Cancel();
        }

        private void OnChangeDirectionAttempt(EntityUid uid, DummyComponent component, ChangeDirectionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, DummyComponent component, UseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnEmoteAttempt(EntityUid uid, DummyComponent component, EmoteAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, DummyComponent component, InteractionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, DummyComponent component, DropAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, DummyComponent component, PickupAttemptEvent args)
        {
            args.Cancel();
        }
    }
}


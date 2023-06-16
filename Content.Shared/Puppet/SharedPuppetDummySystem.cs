using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Emoting;
using Content.Shared.Movement.Events;

namespace Content.Shared.Puppet
{
    public abstract class SharedPuppetDummySystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PuppetDummyComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
            SubscribeLocalEvent<PuppetDummyComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, PuppetDummyComponent component, ComponentStartup args)
        {
            _blocker.UpdateCanMove(uid);
        }

        private void OnMoveAttempt(EntityUid uid, PuppetDummyComponent component, UpdateCanMoveEvent args)
        {
            if (component.LifeStage > ComponentLifeStage.Running)
                return;

            args.Cancel();
        }

        private void OnChangeDirectionAttempt(EntityUid uid, PuppetDummyComponent component, ChangeDirectionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, PuppetDummyComponent component, UseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnEmoteAttempt(EntityUid uid, PuppetDummyComponent component, EmoteAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, PuppetDummyComponent component, InteractionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, PuppetDummyComponent component, DropAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, PuppetDummyComponent component, PickupAttemptEvent args)
        {
            args.Cancel();
        }
    }
}


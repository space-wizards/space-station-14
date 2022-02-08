using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Buckle
{
    public abstract class SharedBuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedBuckleComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<SharedBuckleComponent, DownAttemptEvent>(HandleDown);
            SubscribeLocalEvent<SharedBuckleComponent, StandAttemptEvent>(HandleStand);
            SubscribeLocalEvent<SharedBuckleComponent, ThrowPushbackAttemptEvent>(HandleThrowPushback);
            SubscribeLocalEvent<SharedBuckleComponent, MovementAttemptEvent>(HandleMove);
            SubscribeLocalEvent<SharedBuckleComponent, ChangeDirectionAttemptEvent>(OnBuckleChangeDirectionAttempt);
        }

        private void OnBuckleChangeDirectionAttempt(EntityUid uid, SharedBuckleComponent component, ChangeDirectionAttemptEvent args)
        {
            if (component.Buckled)
                args.Cancel();
        }

        private void HandleMove(EntityUid uid, SharedBuckleComponent component, MovementAttemptEvent args)
        {
            if (component.Buckled)
                args.Cancel();
        }

        private void HandleStand(EntityUid uid, SharedBuckleComponent component, StandAttemptEvent args)
        {
            if (component.Buckled)
            {
                args.Cancel();
            }
        }

        private void HandleDown(EntityUid uid, SharedBuckleComponent component, DownAttemptEvent args)
        {
            if (component.Buckled)
            {
                args.Cancel();
            }
        }

        private void HandleThrowPushback(EntityUid uid, SharedBuckleComponent component, ThrowPushbackAttemptEvent args)
        {
            if (!component.Buckled) return;
            args.Cancel();
        }

        private void PreventCollision(EntityUid uid, SharedBuckleComponent component, PreventCollideEvent args)
        {
            if (args.BodyB.Owner != component.LastEntityBuckledTo) return;

            if (component.Buckled || component.DontCollide)
            {
                args.Cancel();
            }
        }
    }
}

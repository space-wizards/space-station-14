using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Buckle
{
    public abstract class SharedBuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedStrapComponent, RotateEvent>(OnStrapRotate);

            SubscribeLocalEvent<SharedBuckleComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<SharedBuckleComponent, DownAttemptEvent>(HandleDown);
            SubscribeLocalEvent<SharedBuckleComponent, StandAttemptEvent>(HandleStand);
            SubscribeLocalEvent<SharedBuckleComponent, ThrowPushbackAttemptEvent>(HandleThrowPushback);
            SubscribeLocalEvent<SharedBuckleComponent, UpdateCanMoveEvent>(HandleMove);
            SubscribeLocalEvent<SharedBuckleComponent, ChangeDirectionAttemptEvent>(OnBuckleChangeDirectionAttempt);
        }

        private void OnStrapRotate(EntityUid uid, SharedStrapComponent component, ref RotateEvent args)
        {
            // TODO: This looks dirty af.
            // On rotation of a strap, reattach all buckled entities.
            // This fixes buckle offsets and draw depths.
            foreach (var buckledEntity in component.BuckledEntities)
            {
                if (!EntityManager.TryGetComponent(buckledEntity, out SharedBuckleComponent? buckled))
                {
                    continue;
                }

                buckled.ReAttach(component);
                Dirty(buckled);
            }
        }

        private void OnBuckleChangeDirectionAttempt(EntityUid uid, SharedBuckleComponent component, ChangeDirectionAttemptEvent args)
        {
            if (component.Buckled)
                args.Cancel();
        }

        private void HandleMove(EntityUid uid, SharedBuckleComponent component, UpdateCanMoveEvent args)
        {
            if (component.LifeStage > ComponentLifeStage.Running)
                return;

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

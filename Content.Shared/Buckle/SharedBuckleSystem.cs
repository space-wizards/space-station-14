using Content.Shared.Buckle.Components;
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
            SubscribeLocalEvent<SharedBuckleComponent, ThrowPushbackAttemptEvent>(HandleThrowPushback);
        }

        private void HandleThrowPushback(EntityUid uid, SharedBuckleComponent component, ThrowPushbackAttemptEvent args)
        {
            if (!component.Buckled) return;
            args.Cancel();
        }

        private void PreventCollision(EntityUid uid, SharedBuckleComponent component, PreventCollideEvent args)
        {
            if (args.BodyB.Owner.Uid != component.LastEntityBuckledTo) return;

            component.IsOnStrapEntityThisFrame = true;
            if (component.Buckled || component.DontCollide)
            {
                args.Cancel();
            }
        }
    }
}

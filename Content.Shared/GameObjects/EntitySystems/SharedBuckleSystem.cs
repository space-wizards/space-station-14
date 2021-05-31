using Content.Shared.GameObjects.Components.Buckle;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedBuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedBuckleComponent, PreventCollideEvent>(PreventCollision);
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

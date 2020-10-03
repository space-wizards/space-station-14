using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PA
{
    public abstract class ParticleAcceleratorPartComponent : Component
    {
        [ViewVariables] public ParticleAccelerator ParticleAccelerator = new ParticleAccelerator();

        public override void Initialize()
        {
            base.Initialize();
            Owner.EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
            if (!Owner.TryGetComponent<CollidableComponent>(out var collidableComponent))
            {
                Logger.Error("ParticleAcceleratorPartComponent created with no CollidableComponent");
                return;
            }
            collidableComponent.AnchoredChanged += ReCalculateParticleAccelerator;
        }

        private void RotateEvent(RotateEvent ev)
        {
            if (ev.Sender != Owner) return;

            ReCalculateParticleAccelerator();
        }

        private void ReCalculateParticleAccelerator()
        {
            if (!Owner.TryGetComponent<CollidableComponent>(out var collidableComponent) ||
                !collidableComponent.Anchored) return;

            UnRegisterAtParticleAccelerator();
            RegisterAtParticleAccelerator();
        }

        protected abstract void RegisterAtParticleAccelerator();

        protected abstract void UnRegisterAtParticleAccelerator();
    }
}

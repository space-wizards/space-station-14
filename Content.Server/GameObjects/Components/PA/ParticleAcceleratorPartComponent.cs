using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PA
{
    public abstract class ParticleAcceleratorPartComponent : Component
    {
        [ViewVariables] public ParticleAccelerator ParticleAccelerator;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
            if (!Owner.TryGetComponent<CollidableComponent>(out var collidableComponent))
            {
                Logger.Error("ParticleAcceleratorPartComponent created with no CollidableComponent");
                return;
            }
            collidableComponent.AnchoredChanged += RebuildParticleAccelerator;
        }

        private void RotateEvent(RotateEvent ev)
        {
            if (ev.Sender != Owner) return;

            RebuildParticleAccelerator();
        }

        public void RebuildParticleAccelerator()
        {
            if (!Owner.TryGetComponent<CollidableComponent>(out var collidableComponent)) return;

            if (!collidableComponent.Anchored)
            {
                if (ParticleAccelerator != null) UnRegisterAtParticleAccelerator();
                ParticleAccelerator = new ParticleAccelerator();
            }
            else
            {
                ParticleAccelerator = new ParticleAccelerator();
                RegisterAtParticleAccelerator();
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (ParticleAccelerator != null) UnRegisterAtParticleAccelerator();
        }

        public abstract ParticleAcceleratorPartComponent[] GetNeighbours();

        protected abstract void RegisterAtParticleAccelerator();

        protected abstract void UnRegisterAtParticleAccelerator();
    }
}

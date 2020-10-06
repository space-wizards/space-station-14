using System.Collections.Generic;
using Content.Server.Utility;
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
        [ViewVariables] public bool dontAddToPa;

        private CollidableComponent _collidableComponent;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EntityManager.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, RotateEvent);
            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("ParticleAcceleratorPartComponent created with no CollidableComponent");
                return;
            }
            _collidableComponent.AnchoredChanged += OnAnchorChanged;
        }

        private void RotateEvent(RotateEvent ev)
        {
            if (ev.Sender != Owner) return;

            RebuildParticleAccelerator();
        }

        public void OnAnchorChanged()
        {
            if(_collidableComponent.Anchored) Owner.SnapToGrid();
            RebuildParticleAccelerator();
        }

        public void RebuildParticleAccelerator()
        {
            if (!_collidableComponent.Anchored)
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
            dontAddToPa = true;
            if (ParticleAccelerator != null) UnRegisterAtParticleAccelerator();
        }

        public abstract ParticleAcceleratorPartComponent[] GetNeighbours();

        protected abstract void RegisterAtParticleAccelerator();

        protected abstract void UnRegisterAtParticleAccelerator();
    }
}

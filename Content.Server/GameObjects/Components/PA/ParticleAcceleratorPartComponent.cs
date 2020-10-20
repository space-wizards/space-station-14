#nullable enable
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
        [ViewVariables] private PhysicsComponent? _collidableComponent;
        [ViewVariables] public ParticleAcceleratorControlBoxComponent? Master;
        [ViewVariables] protected SnapGridComponent? SnapGrid;

        public override void Initialize()
        {
            base.Initialize();
            // FIXME: this has to be an entity system, full stop.
            if (!Owner.TryGetComponent(out _collidableComponent))
            {
                Logger.Error("ParticleAcceleratorPartComponent created with no CollidableComponent");
            }
            else
            {
                _collidableComponent.AnchoredChanged += OnAnchorChanged;
            }

            if (!Owner.TryGetComponent(out SnapGrid))
            {
                Logger.Error("ParticleAcceleratorControlBox was created without SnapGridComponent");
            }
        }

        public void OnAnchorChanged()
        {
            RescanIfPossible();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            RescanIfPossible();
        }

        private void RescanIfPossible()
        {
            Master?.RescanParts();
        }

        public virtual void Rotated()
        {
            RescanIfPossible();
        }
    }
}

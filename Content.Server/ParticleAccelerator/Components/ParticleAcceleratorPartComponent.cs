#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.ParticleAccelerator.Components
{
    public abstract class ParticleAcceleratorPartComponent : Component
    {
        [ViewVariables] public ParticleAcceleratorControlBoxComponent? Master;

        protected override void Initialize()
        {
            base.Initialize();
            // FIXME: this has to be an entity system, full stop.

            Owner.Transform.Anchored = true;
        }

        public void OnAnchorChanged()
        {
            RescanIfPossible();
        }

        protected override void OnRemove()
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

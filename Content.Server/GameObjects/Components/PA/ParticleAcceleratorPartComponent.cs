#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PA
{
    public abstract class ParticleAcceleratorPartComponent : Component
    {
        [ViewVariables] public ParticleAcceleratorControlBoxComponent? Master;
        [ViewVariables] protected SnapGridComponent? SnapGrid;

        public override void Initialize()
        {
            base.Initialize();
            // FIXME: this has to be an entity system, full stop.

            Owner.EnsureComponent<SnapGridComponent>(out SnapGrid);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    OnAnchorChanged();
                    break;
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

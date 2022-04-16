using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Anchored = true;
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
            if (Master == null || IoCManager.Resolve<IEntityManager>()
                    .TryGetComponent<MetaDataComponent>(Master.Owner, out var meta) ||
                meta.EntityLifeStage >= EntityLifeStage.Terminating) return;

            Master.RescanParts();
        }

        public virtual void Rotated()
        {
            RescanIfPossible();
        }
    }
}

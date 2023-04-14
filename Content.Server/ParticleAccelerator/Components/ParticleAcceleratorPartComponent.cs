namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    [Virtual]
    public class ParticleAcceleratorPartComponent : Component
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
            Master?.RescanParts();
        }

        public void Moved()
        {
            RescanIfPossible();
        }
    }
}

using Content.Shared.GameObjects.Components.Recycling;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Recycling
{
    [RegisterComponent]
    public class RecyclerComponent : Component, ICollideBehavior
    {
        public override string Name => "Recycler";

        private bool Safe { get; set; } = true;

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            var species = collidedWith.HasComponent<SpeciesComponent>();
            if (species && !Safe)
            {
                return;
            }

            collidedWith.Delete(); // TODO: Gib / recycle

            if (species && Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }
    }
}

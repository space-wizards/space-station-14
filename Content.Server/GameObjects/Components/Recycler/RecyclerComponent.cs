using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Recycler
{
    [RegisterComponent]
    public class RecyclerComponent : Component, ICollideBehavior
    {
        public override string Name => "Recycler";

        private bool Safe { get; set; } = true;

        public void CollideWith(IEntity collidedWith)
        {
            if (collidedWith.HasComponent<SpeciesComponent>() && Safe)
            {
                return;
            }

            collidedWith.Delete();
        }
    }
}

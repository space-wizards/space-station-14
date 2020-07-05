using Content.Shared.GameObjects.Components.Recycling;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Recycling
{
    [RegisterComponent]
    public class RecyclerComponent : Component, ICollideBehavior
    {
        public override string Name => "Recycler";

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables]
        private bool _safe;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _safe, "safe", true);
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            // TODO: Prevent collision with recycled items
            var species = collidedWith.HasComponent<SpeciesComponent>();
            if (species && _safe)
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

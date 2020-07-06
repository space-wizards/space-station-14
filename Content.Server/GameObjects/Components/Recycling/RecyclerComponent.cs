using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Recycling;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
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

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables]
        private int _efficiency; // TODO

        private void Bloodstain()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }

        private void Clean()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        private void Recycle(IEntity entity)
        {
            // TODO: Prevent collision with recycled items
            var species = entity.HasComponent<SpeciesComponent>();
            if (species && !_safe)
            {
                entity.Delete(); // TODO: Gib
                Bloodstain();
                return;
            }

            var constructionSystem = EntitySystem.Get<ConstructionSystem>();
            var entityId = entity.MetaData.EntityPrototype?.ID;

            if (entityId == null ||
                !constructionSystem.CraftRecipes.TryGetValue(entityId, out var prototype))
            {
                return;
            }

            var recyclerPosition = Owner.Transform.MapPosition;
            var lastStep = prototype.Stages[^2].Forward as ConstructionStepMaterial;

            constructionSystem.SpawnIngredient(recyclerPosition, lastStep);
            entity.Delete();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _safe, "safe", true);
            serializer.DataField(ref _efficiency, "efficiency", 25);
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            Recycle(collidedWith);
        }
    }
}

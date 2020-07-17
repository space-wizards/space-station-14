using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
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
    // TODO: Add sound and safe beep
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

        private bool Powered =>
            !Owner.TryGetComponent(out PowerReceiverComponent receiver) ||
            receiver.Powered;

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

        private bool CanGib(IEntity entity)
        {
            return entity.HasComponent<SpeciesComponent>() &&
                   !_safe &&
                   Powered;
        }

        private bool CanRecycle(IEntity entity, [MaybeNullWhen(false)] out ConstructionPrototype prototype)
        {
            prototype = null;

            var constructionSystem = EntitySystem.Get<ConstructionSystem>();
            var entityId = entity.MetaData.EntityPrototype?.ID;

            if (entityId == null ||
                !constructionSystem.CraftRecipes.TryGetValue(entityId, out prototype))
            {
                return false;
            }

            return Powered;
        }

        private void Recycle(IEntity entity)
        {
            // TODO: Prevent collision with recycled items
            if (CanGib(entity))
            {
                entity.Delete(); // TODO: Gib
                Bloodstain();
                return;
            }

            if (!CanRecycle(entity, out var prototype))
            {
                return;
            }

            var recyclerPosition = Owner.Transform.MapPosition;
            var lastStep = prototype.Stages[^2].Forward as ConstructionStepMaterial;

            var constructionSystem = EntitySystem.Get<ConstructionSystem>();
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

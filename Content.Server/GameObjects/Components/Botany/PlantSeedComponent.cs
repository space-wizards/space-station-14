using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    class PlantSeedComponent : Component
    {
        public override string Name => "PlantSeed";

        public PlantDNA DNA;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref DNA, "DNA", new PlantDNA());
        }

        public void PlantIntoHolder(PlantHolderComponent holder)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            //It might be better to construct an entity from scratch, but this method at least forces you to ensure that ExposeData works
            entityManager.TrySpawnEntityAt("BasePlant", holder.Owner.Transform.GridPosition, out var plant);

            var plantComponent = plant.GetComponent<PlantComponent>();
            plantComponent.DNA = (PlantDNA)DNA.Clone();
            holder.HeldPlant = plantComponent;
            plantComponent.UpdateCurrentStage();
            plant.GetComponent<SpriteComponent>().DrawDepth = DrawDepth.Objects;
            Owner.Delete();
        }
    }
}

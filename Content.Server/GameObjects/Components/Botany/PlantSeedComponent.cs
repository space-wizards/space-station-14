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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public void PlantIntoHolder(PlantHolderComponent holder)
        {
            if (!Owner.HasComponent<PlantDNAComponent>())
            {
                throw new NotImplementedException();
            }
            var entityManager = IoCManager.Resolve<IEntityManager>();

            //It might be better to construct an entity from scratch, but this method at least forces you to ensure that ExposeData works
            entityManager.TrySpawnEntityAt("BasePlant", holder.Owner.Transform.GridPosition, out var plant);

            if (!plant.HasComponent<PlantDNAComponent>())
            {
                plant.AddComponent<PlantDNAComponent>();
            }
            var dna = plant.GetComponent<PlantDNAComponent>();
            dna.DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();

            var plantComponent = plant.GetComponent<PlantComponent>();
            holder.HeldPlant = plantComponent;
            plantComponent.ChangeStage(dna.DNA.Lifecycle.StartNodeID);
            plant.GetComponent<SpriteComponent>().DrawDepth = DrawDepth.Objects;
            plantComponent.UpdateSprite();
            Owner.Delete();
        }
    }
}

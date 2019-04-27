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
        public override string Name => "PlantSeedComponent";

        public PlantDNA DNA;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref DNA, "DNA", new PlantDNA());
        }

        public IEntity CreatePlant()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.TrySpawnEntityAt("default_plant", Owner.Transform.Parent.GridPosition, out var plant);
            var plantComponent = plant.GetComponent<PlantComponent>();
            plantComponent.DNA = (PlantDNA)DNA.Clone();
            plantComponent.UpdateSprite();
            return plant;
        }
    }
}

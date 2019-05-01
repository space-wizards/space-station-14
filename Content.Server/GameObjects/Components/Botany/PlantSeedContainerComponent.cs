using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    /// <summary>
    /// For entities that can be interacted with in some way in order to spawn seeds e.g. eating apples gives seeds
    /// </summary>
    class PlantSeedContainerComponent : Component, IUse
    {
        public override string Name => "PlantSeedContainer";

        [ViewVariables(VVAccess.ReadWrite)]
        public PlantDNA DNA;
        [ViewVariables(VVAccess.ReadWrite)]
        public string seedPrototype;

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.TrySpawnEntityAt(seedPrototype, Owner.Transform.GridPosition, out var seed);
            seed.GetComponent<PlantSeedComponent>().DNA = (PlantDNA)DNA.Clone();

            Owner.Delete();
            return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref DNA, "DNA", null);
            serializer.DataField(ref seedPrototype, "seedPrototype", "example_seed");
        }
    }
}

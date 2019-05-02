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
    class ExtractableSeedComponent : Component, IUse
    {
        public override string Name => "ExtractableSeed";

        [ViewVariables(VVAccess.ReadWrite)]
        public string seedPrototype;

        // TODO: Realistic ways to turn into a seed ie. tools, machine, picking by hand
        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.TrySpawnEntityAt(seedPrototype, Owner.Transform.GridPosition, out var seed);
            if (seed.TryGetComponent<PlantDNAComponent>(out var dna))
            {
                dna.DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
            }
            else
            {
                seed.AddComponent<PlantDNAComponent>().DNA = (PlantDNA)Owner.GetComponent<PlantDNAComponent>().DNA.Clone();
            }

            Owner.Delete();
            return true;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref seedPrototype, "seedPrototype", "example_seed");
        }
    }
}

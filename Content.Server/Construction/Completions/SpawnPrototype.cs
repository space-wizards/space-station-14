using System;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    public class SpawnPrototype : IEdgeCompleted
    {
        public string Prototype { get; private set; }
        public int Amount { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

        public async Task Completed(IEntity entity)
        {
            if (entity.Deleted) return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var coordinates = entity.Transform.Coordinates;

            for (var i = 0; i < Amount; i++)
            {
                entityManager.SpawnEntity(Prototype, coordinates);
            }
        }
    }
}

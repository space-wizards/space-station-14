#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SpawnPrototype : IGraphAction
    {
        public string Prototype { get; private set; } = string.Empty;
        public int Amount { get; private set; } = 1;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(Prototype)) return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var coordinates = entity.Transform.Coordinates;

            for (var i = 0; i < Amount; i++)
            {
                entityManager.SpawnEntity(Prototype, coordinates);
            }
        }
    }
}

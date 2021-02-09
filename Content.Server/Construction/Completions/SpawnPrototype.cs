#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Construction;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SpawnPrototype : IGraphAction
    {
        public string Prototype { get; private set; } = string.Empty;
        public int Amount { get; private set; } = 1;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }


        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(Prototype)) return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var coordinates = entity.Transform.Coordinates;

            if (EntityPrototypeHelpers.HasComponent<StackComponent>(Prototype))
            {
                var _entity = entityManager.SpawnEntity(Prototype, coordinates);
                StackComponent stackComponent = _entity.GetComponent<StackComponent>();

                stackComponent.Count = Math.Min(stackComponent.MaxCount, Amount);
            }
            else
            {
                for (var i = 0; i < Amount; i++)
                {
                    entityManager.SpawnEntity(Prototype, coordinates);
                }
            }

        }
    }
}

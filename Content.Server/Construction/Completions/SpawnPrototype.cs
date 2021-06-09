#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpawnPrototype : IGraphAction
    {
        [DataField("prototype")] public string Prototype { get; private set; } = string.Empty;
        [DataField("amount")] public int Amount { get; private set; } = 1;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(Prototype)) return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var coordinates = entity.Transform.Coordinates;

            if (EntityPrototypeHelpers.HasComponent<StackComponent>(Prototype))
            {
                var stack = entityManager.SpawnEntity(Prototype, coordinates);
                stack.EntityManager.EventBus.RaiseLocalEvent(stack.Uid, new StackChangeCountEvent(Amount), false);
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

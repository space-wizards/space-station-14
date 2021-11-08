using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.Prototypes;
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

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Prototype))
                return;

            var coordinates = entityManager.GetComponent<TransformComponent>(uid).Coordinates;

            if (EntityPrototypeHelpers.HasComponent<StackComponent>(Prototype))
            {
                var stackEnt = entityManager.SpawnEntity(Prototype, coordinates);
                var stack = stackEnt.GetComponent<StackComponent>();
                entityManager.EntitySysManager.GetEntitySystem<StackSystem>().SetCount(stackEnt.Uid, Amount, stack);
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

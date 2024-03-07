using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SpawnPrototypeAtContainer : IGraphAction
    {
        [DataField("prototype")] public string Prototype { get; private set; } = string.Empty;
        [DataField("container")] public string Container { get; private set; } = string.Empty;
        [DataField("amount")] public int Amount { get; private set; } = 1;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Container) || string.IsNullOrEmpty(Prototype))
                return;

            var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
            var container = containerSystem.EnsureContainer<Container>(uid, Container);

            var coordinates = entityManager.GetComponent<TransformComponent>(uid).Coordinates;
            for (var i = 0; i < Amount; i++)
            {
                containerSystem.Insert(entityManager.SpawnEntity(Prototype, coordinates), container);
            }
        }
    }
}

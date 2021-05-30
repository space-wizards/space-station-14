#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpawnPrototypeAtContainer : IGraphAction
    {
        [DataField("prototype")] public string Prototype { get; } = string.Empty;
        [DataField("container")] public string Container { get; } = string.Empty;
        [DataField("amount")] public int Amount { get; } = 1;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(Container) || string.IsNullOrEmpty(Prototype))
                return;

            var container = entity.EnsureContainer<Container>(Container);

            for (var i = 0; i < Amount; i++)
            {
                container.Insert(entity.EntityManager.SpawnEntity(Prototype, entity.Transform.Coordinates));
            }
        }
    }
}

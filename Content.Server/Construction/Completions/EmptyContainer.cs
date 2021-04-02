#nullable enable
using System.Linq;
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
    public class EmptyContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            // TODO: Use container helpers.
            foreach (var contained in container.ContainedEntities.ToArray())
            {
                container.ForceRemove(contained);
                contained.Transform.Coordinates = entity.Transform.Coordinates;
                contained.Transform.AttachToGridOrMap();
            }
        }
    }
}

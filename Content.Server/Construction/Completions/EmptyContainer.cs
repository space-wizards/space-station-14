#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class EmptyContainer : IGraphAction
    {
        public string Container { get; private set; } = string.Empty;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Container, "container", string.Empty);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            foreach (var ent in container.ContainedEntities.ToArray())
            {
                if (ent == null || ent.Deleted) continue;
                container.ForceRemove(ent);
                ent.Transform.Coordinates = entity.Transform.Coordinates;
            }
        }
    }
}

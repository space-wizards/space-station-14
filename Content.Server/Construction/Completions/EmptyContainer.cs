#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class EmptyContainer : IGraphAction
    {
        public string Container { get; private set; } = string.Empty;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Container, "container", string.Empty);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            container.EmptyContainer(true, entity.Transform.Coordinates);
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class EmptyContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            // TODO: Use container system methods.
            var transform = entityManager.GetComponent<ITransformComponent>(uid);
            foreach (var contained in container.ContainedEntities.ToArray())
            {
                container.ForceRemove(contained);
                contained.Transform.Coordinates = transform.Coordinates;
                contained.Transform.AttachToGridOrMap();
            }
        }
    }
}

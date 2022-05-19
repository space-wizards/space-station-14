using System.Linq;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class EmptyContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return;

            // TODO: Use container system methods.
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            foreach (var contained in container.ContainedEntities.ToArray())
            {
                container.ForceRemove(contained);
                var cTransform = entityManager.GetComponent<TransformComponent>(contained);
                cTransform.Coordinates = transform.Coordinates;
                cTransform.AttachToGridOrMap();
            }
        }
    }
}

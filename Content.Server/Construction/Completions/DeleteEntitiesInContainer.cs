using System.Linq;
using Content.Shared.Construction;
using Robust.Shared.Containers;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public sealed class DeleteEntitiesInContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; } = string.Empty;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Container)) return;
            // TODO CONSTRUCTION: Use the new ContainerSystem methods here.
            if (!entityManager.TryGetComponent(uid, out ContainerManagerComponent? containerMan)) return;
            if (!containerMan.TryGetContainer(Container, out var container)) return;

            foreach (var contained in container.ContainedEntities.ToArray())
            {
                if(container.Remove(contained))
                    entityManager.QueueDeleteEntity(contained);
            }
        }
    }
}

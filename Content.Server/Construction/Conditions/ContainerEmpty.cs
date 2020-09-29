#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class ContainerEmpty : IEdgeCondition
    {
        public string Container { get; private set; } = string.Empty;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Container, "container", string.Empty);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return true;

            return container.ContainedEntities.Count == 0;
        }
    }
}

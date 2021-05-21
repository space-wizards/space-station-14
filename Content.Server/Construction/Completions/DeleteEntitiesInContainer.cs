#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public class DeleteEntitiesInContainer : IGraphAction
    {
        [DataField("container")] public string Container { get; } = string.Empty;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (string.IsNullOrEmpty(Container)) return;
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerMan)) return;
            if (!containerMan.TryGetContainer(Container, out var container)) return;

            foreach (var contained in container.ContainedEntities.ToArray())
            {
                if(container.Remove(contained))
                    contained.Delete();
            }
        }
    }
}

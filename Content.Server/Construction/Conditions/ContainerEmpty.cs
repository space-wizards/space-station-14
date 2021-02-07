#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class ContainerEmpty : IEdgeCondition
    {
        public string Container { get; private set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Container, "container", string.Empty);
            serializer.DataField(this, x => x.Text, "text", string.Empty);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return true;

            return container.ContainedEntities.Count == 0;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return false;

            if (container.ContainedEntities.Count == 0)
                return false;

            message.AddMarkup(Text);
            return true;

        }
    }
}

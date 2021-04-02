#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ContainerEmpty : IGraphCondition
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;
        [DataField("text")] public string Text { get; private set; } = string.Empty;

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

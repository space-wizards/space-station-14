#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AddContainer : IGraphAction
    {
        [DataField("container")] public string? Container { get; private set; } = null;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(Container))
                return;

            var construction = entity.GetComponent<ConstructionComponent>();
            construction.AddContainer(Container);
        }
    }
}

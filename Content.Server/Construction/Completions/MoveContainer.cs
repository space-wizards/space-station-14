#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class MoveContainer : IGraphAction
    {
        [DataField("from")] public string? FromContainer { get; } = null;
        [DataField("to")] public string? ToContainer { get; } = null;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (string.IsNullOrEmpty(FromContainer) || string.IsNullOrEmpty(ToContainer))
                return;

            var from = entity.EnsureContainer<Container>(FromContainer);
            var to = entity.EnsureContainer<Container>(ToContainer);

            foreach (var contained in from.ContainedEntities.ToArray())
            {
                if (from.Remove(contained))
                    to.Insert(contained);
            }
        }
    }
}

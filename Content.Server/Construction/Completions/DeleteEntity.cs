using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DeleteEntity : IGraphAction
    {
        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            entity.Delete();
        }
    }
}

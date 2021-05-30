#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Threading.Tasks;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DestroyEntity : IGraphAction
    {
        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            var destructibleSystem = EntitySystem.Get<DestructibleSystem>();
            destructibleSystem.ActSystem.HandleDestruction(entity);
        }
    }
}

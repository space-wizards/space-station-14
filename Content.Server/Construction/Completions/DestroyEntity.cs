using Content.Server.Destructible;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DestroyEntity : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetEntity(uid, out var entity))
                return; // This should never happen, but.

            var destructibleSystem = EntitySystem.Get<DestructibleSystem>();
            destructibleSystem.ActSystem.HandleDestruction(entity.Uid);
        }
    }
}

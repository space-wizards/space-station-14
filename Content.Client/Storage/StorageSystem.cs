using Content.Shared.Storage;
using Content.Client.Animations;

namespace Content.Client.Storage;

// TODO kill this is all horrid.
public sealed class StorageSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
    }

    /// <summary>
    /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
    /// </summary>
    /// <param name="msg"></param>
    public void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesEvent msg)
    {
        if (!TryComp(msg.Storage, out ClientStorageComponent? storage))
            return;

        for (var i = 0; msg.StoredEntities.Count > i; i++)
        {
            var entity = msg.StoredEntities[i];
            var initialPosition = msg.EntityPositions[i];
            if (_entityManager.EntityExists(entity) && TryComp(msg.Storage, out TransformComponent? transformComp))
            {
                ReusableAnimations.AnimateEntityPickup(entity, initialPosition, transformComp.LocalPosition, _entityManager);
            }
        }
    }
}

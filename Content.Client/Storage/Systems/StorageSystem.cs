using Content.Client.Animations;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Client.Storage.Systems;

// TODO kill this is all horrid.
public sealed class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action<EntityUid, StorageComponent>? StorageUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
    }

    public override void UpdateUI(EntityUid uid, StorageComponent component)
    {
        // Should we wrap this in some prediction call maybe?
        StorageUpdated?.Invoke(uid, component);
    }

    /// <summary>
    /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
    /// </summary>
    /// <param name="msg"></param>
    public void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesEvent msg)
    {
        TryComp(GetEntity(msg.Storage), out TransformComponent? transformComp);

        for (var i = 0; msg.StoredEntities.Count > i; i++)
        {
            var entity = GetEntity(msg.StoredEntities[i]);

            var initialPosition = msg.EntityPositions[i];
            if (EntityManager.EntityExists(entity) && transformComp != null)
            {
                ReusableAnimations.AnimateEntityPickup(entity, GetCoordinates(initialPosition), transformComp.LocalPosition, msg.EntityAngles[i], EntityManager);
            }
        }
    }
}

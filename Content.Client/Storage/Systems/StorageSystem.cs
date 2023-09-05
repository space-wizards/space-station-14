using Content.Client.Animations;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Client.Storage.Systems;

// TODO kill this is all horrid.
public sealed class StorageSystem : SharedStorageSystem
{
    public event Action<EntityUid, StorageComponent>? StorageUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
        SubscribeLocalEvent<StorageComponent, AfterAutoHandleStateEvent>(OnStorageAfter);
    }

    private void OnStorageAfter(EntityUid uid, StorageComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(uid, component);
    }

    public override void UpdateUI(EntityUid uid, StorageComponent component)
    {
        StorageUpdated?.Invoke(uid, component);
    }

    /// <summary>
    /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
    /// </summary>
    /// <param name="msg"></param>
    public void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesEvent msg)
    {
        TryComp(msg.Storage, out TransformComponent? transformComp);

        for (var i = 0; msg.StoredEntities.Count > i; i++)
        {
            var entity = msg.StoredEntities[i];
            var initialPosition = msg.EntityPositions[i];
            if (EntityManager.EntityExists(entity) && transformComp != null)
            {
                ReusableAnimations.AnimateEntityPickup(entity, initialPosition, transformComp.LocalPosition, msg.EntityAngles[i], EntityManager);
            }
        }
    }
}

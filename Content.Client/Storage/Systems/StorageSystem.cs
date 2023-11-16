using Content.Client.Animations;
using Content.Shared.Hands;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Storage.Systems;

// TODO kill this is all horrid.
public sealed class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityPickupAnimationSystem _entityPickupAnimation = default!;

    public event Action<EntityUid, StorageComponent>? StorageUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
    }

    public override void UpdateUI(Entity<StorageComponent?> entity)
    {
        if (Resolve(entity.Owner, ref entity.Comp))
            StorageUpdated?.Invoke(entity.Owner, entity.Comp);
    }

    /// <inheritdoc />
    public override void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates, EntityCoordinates finalCoordinates,
        Angle initialRotation, EntityUid? user = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        PickupAnimation(uid, initialCoordinates, finalCoordinates, initialRotation);
    }

    private void HandlePickupAnimation(PickupAnimationEvent msg)
    {
        PickupAnimation(GetEntity(msg.ItemUid), GetCoordinates(msg.InitialPosition), GetCoordinates(msg.FinalPosition), msg.InitialAngle);
    }

    public void PickupAnimation(EntityUid item, EntityCoordinates initialCoords, EntityCoordinates finalCoords, Angle initialAngle)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (finalCoords.InRange(EntityManager, _transform, initialCoords, 0.1f) ||
            !Exists(initialCoords.EntityId) || !Exists(finalCoords.EntityId))
        {
            return;
        }

        var finalMapPos = finalCoords.ToMapPos(EntityManager, _transform);
        var finalPos = _transform.GetInvWorldMatrix(initialCoords.EntityId).Transform(finalMapPos);

        _entityPickupAnimation.AnimateEntityPickup(item, initialCoords, finalPos, initialAngle);
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
                _entityPickupAnimation.AnimateEntityPickup(entity, GetCoordinates(initialPosition), transformComp.LocalPosition, msg.EntityAngles[i]);
            }
        }
    }
}

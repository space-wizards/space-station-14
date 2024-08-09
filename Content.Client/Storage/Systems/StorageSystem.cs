using System.Linq;
using System.Numerics;
using Content.Client.Animations;
using Content.Shared.Hands;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Storage.Systems;

public sealed class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityPickupAnimationSystem _entityPickupAnimation = default!;

    private readonly List<Entity<StorageComponent>> _openStorages = new();
    public int OpenStorageAmount => _openStorages.Count;

    public event Action<Entity<StorageComponent>>? StorageUpdated;
    public event Action<Entity<StorageComponent>?>? StorageOrderChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        SubscribeAllEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
    }

    public override void UpdateUI(Entity<StorageComponent?> entity)
    {
        if (Resolve(entity.Owner, ref entity.Comp))
            StorageUpdated?.Invoke((entity, entity.Comp));
    }

    public void OpenStorageWindow(Entity<StorageComponent> entity)
    {
        if (_openStorages.Contains(entity))
        {
            if (_openStorages.LastOrDefault() == entity)
            {
                CloseStorageWindow((entity, entity.Comp));
            }
            else
            {
                var storages = new ValueList<Entity<StorageComponent>>(_openStorages);
                var reverseStorages = storages.Reverse();

                foreach (var storageEnt in reverseStorages)
                {
                    if (storageEnt == entity)
                        break;

                    CloseStorageBoundUserInterface(storageEnt.Owner);
                    _openStorages.Remove(entity);
                }
            }
            return;
        }

        ClearNonParentStorages(entity);
        _openStorages.Add(entity);
        Entity<StorageComponent>? last = _openStorages.LastOrDefault();
        StorageOrderChanged?.Invoke(last);
    }

    public void CloseStorageWindow(Entity<StorageComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!_openStorages.Contains((entity, entity.Comp)))
            return;

        var storages = new ValueList<Entity<StorageComponent>>(_openStorages);
        var reverseStorages = storages.Reverse();

        foreach (var storage in reverseStorages)
        {
            CloseStorageBoundUserInterface(storage.Owner);
            _openStorages.Remove(storage);
            if (storage.Owner == entity.Owner)
                break;
        }

        Entity<StorageComponent>? last = null;
        if (_openStorages.Any())
            last = _openStorages.LastOrDefault();
        StorageOrderChanged?.Invoke(last);
    }

    private void ClearNonParentStorages(EntityUid uid)
    {
        var storages = new ValueList<Entity<StorageComponent>>(_openStorages);
        var reverseStorages = storages.Reverse();

        foreach (var storage in reverseStorages)
        {
            if (storage.Comp.Container.Contains(uid))
                break;

            CloseStorageBoundUserInterface(storage.Owner);
            _openStorages.Remove(storage);
        }
    }

    private void CloseStorageBoundUserInterface(Entity<UserInterfaceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.ClientOpenInterfaces.GetValueOrDefault(StorageComponent.StorageUiKey.Key) is not { } bui)
            return;

        bui.Close();
    }

    private void OnShutdown(Entity<StorageComponent> ent, ref ComponentShutdown args)
    {
        CloseStorageWindow((ent, ent.Comp));
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
        
        if (TransformSystem.InRange(finalCoords, initialCoords, 0.1f) ||
            !Exists(initialCoords.EntityId) || !Exists(finalCoords.EntityId))
        {
            return;
        }

        var finalMapPos = TransformSystem.ToMapCoordinates(finalCoords).Position;
        var finalPos = Vector2.Transform(finalMapPos, TransformSystem.GetInvWorldMatrix(initialCoords.EntityId));

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
            if (Exists(entity) && transformComp != null)
            {
                _entityPickupAnimation.AnimateEntityPickup(entity, GetCoordinates(initialPosition), transformComp.LocalPosition, msg.EntityAngles[i]);
            }
        }
    }
}

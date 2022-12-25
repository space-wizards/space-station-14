using System.Linq;
using Content.Server.Lock;
using Content.Server.Mind.Components;
using Content.Server.Resist;
using Content.Server.Station.Components;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Coordinates;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems;

public sealed class BluespaceLockerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceLockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageBeforeOpenEvent>(PreOpen);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageAfterCloseEvent>(PostClose);
    }

    private void OnStartup(EntityUid uid, BluespaceLockerComponent component, ComponentStartup args)
    {
        GetTargetStorage(component);
    }

    private void PreOpen(EntityUid uid, BluespaceLockerComponent component, StorageBeforeOpenEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        // Select target
        var targetContainerStorageComponent = GetTargetStorage(component);
        if (targetContainerStorageComponent == null)
            return;
        BluespaceLockerComponent? targetContainerBluespaceComponent = null;

        // Close target if it is open
        if (targetContainerStorageComponent.Open)
            _entityStorage.CloseStorage(targetContainerStorageComponent.Owner, targetContainerStorageComponent);

        // Apply bluespace effects if target is not a bluespace locker, otherwise let it handle it
        if (!Resolve(targetContainerStorageComponent.Owner, ref targetContainerBluespaceComponent, false))
        {
            // Move contained items
            if (component.TransportEntities)
                foreach (var entity in targetContainerStorageComponent.Contents.ContainedEntities.ToArray())
                {
                    if (!component.AllowSentient && EntityManager.HasComponent<MindComponent>(entity))
                        continue;
                    entityStorageComponent.Contents.Insert(entity, EntityManager);
                }

            // Move contained air
            if (component.TransportGas)
            {
                entityStorageComponent.Air.CopyFromMutable(targetContainerStorageComponent.Air);
                targetContainerStorageComponent.Air.Clear();
            }
        }
    }

    private bool ValidLink(BluespaceLockerComponent component, EntityStorageComponent link)
    {
        return link.Owner.Valid && link.LifeStage != ComponentLifeStage.Deleted;
    }

    private bool ValidAutolink(BluespaceLockerComponent component, EntityStorageComponent link)
    {
        if (!ValidLink(component, link))
            return false;

        if (component.PickLinksFromSameMap &&
            link.Owner.ToCoordinates().GetMapId(_entityManager) == component.Owner.ToCoordinates().GetMapId(_entityManager))
            return false;

        if (component.PickLinksFromStationGrids &&
            !_entityManager.HasComponent<StationMemberComponent>(link.Owner.ToCoordinates().GetGridUid(_entityManager)))
            return false;

        if (component.PickLinksFromResistLockers &&
            !_entityManager.HasComponent<ResistLockerComponent>(link.Owner))
            return false;

        return true;
    }

    private EntityStorageComponent? GetTargetStorage(BluespaceLockerComponent component)
    {
        while (true)
        {
            // Ensure MinBluespaceLinks
            if (component.BluespaceLinks.Count < component.MinBluespaceLinks)
            {
                // Get an shuffle the list of all EntityStorages
                var storages = _entityManager.EntityQuery<EntityStorageComponent>().ToArray();
                _robustRandom.Shuffle(storages);

                // Add valid candidates till MinBluespaceLinks is met
                foreach (var storage in storages)
                {
                    if (!ValidAutolink(component, storage))
                        continue;

                    component.BluespaceLinks.Add(storage);
                    if (component.AutoLinksBidirectional)
                    {
                        _entityManager.EnsureComponent<BluespaceLockerComponent>(storage.Owner, out var targetBluespaceComponent);
                        targetBluespaceComponent.BluespaceLinks.Add(_entityManager.GetComponent<EntityStorageComponent>(component.Owner));
                    }
                    if (component.BluespaceLinks.Count >= component.MinBluespaceLinks)
                        break;
                }
            }

            // If there are no possible link targets and no links, return null
            if (component.BluespaceLinks.Count == 0)
                return null;

            // Attempt to select, validate, and return a link
            var links = component.BluespaceLinks.ToArray();
            var link = links[_robustRandom.Next(0, component.BluespaceLinks.Count)];
            if (ValidLink(component, link))
                return link;
            component.BluespaceLinks.Remove(link);
        }
    }


    private void PostClose(EntityUid uid, BluespaceLockerComponent component, StorageAfterCloseEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        // Select target
        var targetContainerStorageComponent = GetTargetStorage(component);
        if (targetContainerStorageComponent == null)
            return;

        // Move contained items
        if (component.TransportEntities)
            foreach (var entity in entityStorageComponent.Contents.ContainedEntities.ToArray())
            {
                if (!component.AllowSentient && EntityManager.HasComponent<MindComponent>(entity))
                    continue;
                targetContainerStorageComponent.Contents.Insert(entity, EntityManager);
            }

        // Move contained air
        if (component.TransportGas)
        {
            targetContainerStorageComponent.Air.CopyFromMutable(entityStorageComponent.Air);
            entityStorageComponent.Air.Clear();
        }

        // Open and empty target
        if (targetContainerStorageComponent.Open)
        {
            _entityStorage.EmptyContents(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
            _entityStorage.ReleaseGas(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
        }
        else
        {
            if (targetContainerStorageComponent.IsWeldedShut)
            {
                // It gets bluespaced open...
                _weldableSystem.ForceWeldedState(targetContainerStorageComponent.Owner, false);
                if (targetContainerStorageComponent.IsWeldedShut)
                    targetContainerStorageComponent.IsWeldedShut = false;
            }
            LockComponent? lockComponent = null;
            if (Resolve(targetContainerStorageComponent.Owner, ref lockComponent, false) && lockComponent.Locked)
                _lockSystem.Unlock(lockComponent.Owner, lockComponent.Owner, lockComponent);

            _entityStorage.OpenStorage(targetContainerStorageComponent.Owner, targetContainerStorageComponent);
        }
    }
}

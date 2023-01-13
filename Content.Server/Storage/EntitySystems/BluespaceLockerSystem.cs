using System.Linq;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lock;
using Content.Server.Mind.Components;
using Content.Server.Resist;
using Content.Server.Station.Components;
using Content.Server.Storage.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Access.Components;
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
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceLockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageBeforeOpenEvent>(PreOpen);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageAfterCloseEvent>(PostClose);
        SubscribeLocalEvent<BluespaceLockerComponent, BluespaceLockerTeleportDelayComplete>(OnBluespaceLockerTeleportDelayComplete);
    }

    private void OnStartup(EntityUid uid, BluespaceLockerComponent component, ComponentStartup args)
    {
        GetTarget(uid, component);

        if (component.BluespaceEffectOnInit)
            BluespaceEffect(uid, component);
    }

    private void BluespaceEffect(EntityUid uid, BluespaceLockerComponent component)
    {
        Spawn(component.BehaviorProperties.BluespaceEffectPrototype, uid.ToCoordinates());
    }

    private void PreOpen(EntityUid uid, BluespaceLockerComponent component, StorageBeforeOpenEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        component.CancelToken?.Cancel();

        // Select target
        var target = GetTarget(uid, component);
        if (target == null)
            return;

        // Close target if it is open
        if (target.Value.storageComponent.Open)
            _entityStorage.CloseStorage(target.Value.uid, target.Value.storageComponent);

        // Apply bluespace effects if target is not a bluespace locker, otherwise let it handle it
        if (target.Value.bluespaceLockerComponent == null)
        {
            // Move contained items
            if (component.BehaviorProperties.TransportEntities || component.BehaviorProperties.TransportSentient)
                foreach (var entity in target.Value.storageComponent.Contents.ContainedEntities.ToArray())
                {
                    if (EntityManager.HasComponent<MindComponent>(entity))
                    {
                        if (component.BehaviorProperties.TransportSentient)
                            entityStorageComponent.Contents.Insert(entity, EntityManager);
                    }
                    else if (component.BehaviorProperties.TransportEntities)
                        entityStorageComponent.Contents.Insert(entity, EntityManager);
                }

            // Move contained air
            if (component.BehaviorProperties.TransportGas)
            {
                entityStorageComponent.Air.CopyFromMutable(target.Value.storageComponent.Air);
                target.Value.storageComponent.Air.Clear();
            }

            // Bluespace effects
            if (component.BehaviorProperties.BluespaceEffectOnTeleportSource)
                BluespaceEffect(target.Value.uid, component);
            if (component.BehaviorProperties.BluespaceEffectOnTeleportTarget)
                BluespaceEffect(uid, component);
        }

        DestroyAfterLimit(uid, component);
    }

    private bool ValidLink(EntityUid locker, EntityUid link, BluespaceLockerComponent lockerComponent)
    {
        return link.Valid && TryComp<EntityStorageComponent>(link, out var linkStorage) && linkStorage.LifeStage != ComponentLifeStage.Deleted && link != locker;
    }

    /// <returns>True if any HashSet in <paramref name="a"/> would grant access to <paramref name="b"/></returns>
    private bool AccessMatch(IReadOnlyCollection<HashSet<string>>? a, IReadOnlyCollection<HashSet<string>>? b)
    {
        if ((a == null || a.Count == 0) && (b == null || b.Count == 0))
            return true;
        if (a != null && a.Any(aSet => aSet.Count == 0))
            return true;
        if (b != null && b.Any(bSet => bSet.Count == 0))
            return true;

        if (a != null && b != null)
            return a.Any(aSet => b.Any(aSet.SetEquals));
        return false;
    }

    private bool ValidAutolink(EntityUid locker, EntityUid link, BluespaceLockerComponent lockerComponent)
    {
        if (!ValidLink(locker, link, lockerComponent))
            return false;

        if (lockerComponent.PickLinksFromSameMap &&
            link.ToCoordinates().GetMapId(_entityManager) != locker.ToCoordinates().GetMapId(_entityManager))
            return false;

        if (lockerComponent.PickLinksFromStationGrids &&
            !HasComp<StationMemberComponent>(link.ToCoordinates().GetGridUid(_entityManager)))
            return false;

        if (lockerComponent.PickLinksFromResistLockers &&
            !HasComp<ResistLockerComponent>(link))
            return false;

        if (lockerComponent.PickLinksFromSameAccess)
        {
            TryComp<AccessReaderComponent>(locker, out var sourceAccess);
            TryComp<AccessReaderComponent>(link, out var targetAccess);
            if (!AccessMatch(sourceAccess?.AccessLists, targetAccess?.AccessLists))
                return false;
        }

        if (HasComp<BluespaceLockerComponent>(link))
        {
            if (lockerComponent.PickLinksFromNonBluespaceLockers)
                return false;
        }
        else
        {
            if (lockerComponent.PickLinksFromBluespaceLockers)
                return false;
        }

        return true;
    }

    private (EntityUid uid, EntityStorageComponent storageComponent, BluespaceLockerComponent? bluespaceLockerComponent)? GetTarget(EntityUid lockerUid, BluespaceLockerComponent component)
    {
        while (true)
        {
            // Ensure MinBluespaceLinks
            if (component.BluespaceLinks.Count < component.MinBluespaceLinks)
            {
                // Get an shuffle the list of all EntityStorages
                var storages = EntityQuery<EntityStorageComponent>().ToArray();
                _robustRandom.Shuffle(storages);

                // Add valid candidates till MinBluespaceLinks is met
                foreach (var storage in storages)
                {
                    var potentialLink = storage.Owner;

                    if (!ValidAutolink(lockerUid, potentialLink, component))
                        continue;

                    component.BluespaceLinks.Add(potentialLink);
                    if (component.AutoLinksBidirectional || component.AutoLinksUseProperties)
                    {
                        var targetBluespaceComponent = EnsureComp<BluespaceLockerComponent>(potentialLink);

                        if (component.AutoLinksBidirectional)
                            targetBluespaceComponent.BluespaceLinks.Add(lockerUid);

                        if (component.AutoLinksUseProperties)
                            targetBluespaceComponent.BehaviorProperties = component.AutoLinkProperties with {};
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
            if (ValidLink(lockerUid, link, component))
                return (link, Comp<EntityStorageComponent>(link), CompOrNull<BluespaceLockerComponent>(link));
            component.BluespaceLinks.Remove(link);
        }
    }

    private void PostClose(EntityUid uid, BluespaceLockerComponent component, StorageAfterCloseEvent args)
    {
        PostClose(uid, component);
    }

    private void OnBluespaceLockerTeleportDelayComplete(EntityUid uid, BluespaceLockerComponent component, BluespaceLockerTeleportDelayComplete args)
    {
        PostClose(uid, component, false);
    }

    private void PostClose(EntityUid uid, BluespaceLockerComponent component, bool doDelay = true)
    {
        EntityStorageComponent? entityStorageComponent = null;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        component.CancelToken?.Cancel();

        // Do delay
        if (doDelay && component.BehaviorProperties.Delay > 0)
        {
            EnsureComp<DoAfterComponent>(uid);
            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(
                new DoAfterEventArgs(uid, component.BehaviorProperties.Delay, component.CancelToken.Token)
                {
                    UserFinishedEvent = new BluespaceLockerTeleportDelayComplete()
                });
            return;
        }

        // Select target
        var target = GetTarget(uid, component);
        if (target == null)
            return;

        // Move contained items
        if (component.BehaviorProperties.TransportEntities || component.BehaviorProperties.TransportSentient)
            foreach (var entity in entityStorageComponent.Contents.ContainedEntities.ToArray())
            {
                if (EntityManager.HasComponent<MindComponent>(entity))
                {
                    if (component.BehaviorProperties.TransportSentient)
                        target.Value.storageComponent.Contents.Insert(entity, EntityManager);
                }
                else if (component.BehaviorProperties.TransportEntities)
                    target.Value.storageComponent.Contents.Insert(entity, EntityManager);
            }

        // Move contained air
        if (component.BehaviorProperties.TransportGas)
        {
            target.Value.storageComponent.Air.CopyFromMutable(entityStorageComponent.Air);
            entityStorageComponent.Air.Clear();
        }

        // Open and empty target
        if (target.Value.storageComponent.Open)
        {
            _entityStorage.EmptyContents(target.Value.uid, target.Value.storageComponent);
            _entityStorage.ReleaseGas(target.Value.uid, target.Value.storageComponent);
        }
        else
        {
            if (target.Value.storageComponent.IsWeldedShut)
            {
                // It gets bluespaced open...
                _weldableSystem.ForceWeldedState(target.Value.uid, false);
                if (target.Value.storageComponent.IsWeldedShut)
                    target.Value.storageComponent.IsWeldedShut = false;
            }
            LockComponent? lockComponent = null;
            if (Resolve(target.Value.uid, ref lockComponent, false) && lockComponent.Locked)
                _lockSystem.Unlock(target.Value.uid, target.Value.uid, lockComponent);

            _entityStorage.OpenStorage(target.Value.uid, target.Value.storageComponent);
        }

        // Bluespace effects
        if (component.BehaviorProperties.BluespaceEffectOnTeleportSource)
            BluespaceEffect(uid, component);
        if (component.BehaviorProperties.BluespaceEffectOnTeleportTarget)
            BluespaceEffect(target.Value.uid, component);

        DestroyAfterLimit(uid, component);
    }

    private void DestroyAfterLimit(EntityUid uid, BluespaceLockerComponent component)
    {
        if (component.BehaviorProperties.ClearLinksEvery != -1)
        {
            component.UsesSinceLinkClear++;
            if (component.BehaviorProperties.ClearLinksEvery >= component.UsesSinceLinkClear)
            {
                component.BluespaceLinks.Clear();
                component.UsesSinceLinkClear = 0;
            }
        }

        if (component.BehaviorProperties.DestroyAfterUses == -1)
            return;

        component.BehaviorProperties.DestroyAfterUses--;
        if (component.BehaviorProperties.DestroyAfterUses > 0)
            return;

        switch (component.BehaviorProperties.DestroyType)
        {
            case BluespaceLockerDestroyType.Explode:
                _explosionSystem.QueueExplosion(uid.ToCoordinates().ToMap(_entityManager),
                    ExplosionSystem.DefaultExplosionPrototypeId, 4, 1, 2, maxTileBreak: 0);
                goto case BluespaceLockerDestroyType.Delete;
            case BluespaceLockerDestroyType.Delete:
                QueueDel(uid);
                break;
            case BluespaceLockerDestroyType.DeleteComponent:
                RemComp<BluespaceLockerComponent>(uid);
                break;
        }
    }

    private sealed class BluespaceLockerTeleportDelayComplete : EntityEventArgs
    {
    }
}

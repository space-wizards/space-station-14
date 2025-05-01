using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Resist;
using Content.Server.Storage.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Mind.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Server.Shuttles.Components;
using Robust.Shared.Physics;

namespace Content.Server.Storage.EntitySystems;

public sealed class BluespaceLockerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceLockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageBeforeOpenEvent>(PreOpen);
        SubscribeLocalEvent<BluespaceLockerComponent, StorageAfterCloseEvent>(PostClose);
        SubscribeLocalEvent<BluespaceLockerComponent, BluespaceLockerDoAfterEvent>(OnDoAfter);
    }

    private void OnStartup(EntityUid uid, BluespaceLockerComponent component, ComponentStartup args)
    {
        GetTarget(uid, component, true);

        if (component.BehaviorProperties.BluespaceEffectOnInit)
            BluespaceEffect(uid, component, component, true);

        EnsureComp<ArrivalsBlacklistComponent>(uid); // To stop people getting to arrivals terminal
    }

    public void BluespaceEffect(EntityUid effectTargetUid, BluespaceLockerComponent effectSourceComponent, BluespaceLockerComponent? effectTargetComponent, bool bypassLimit = false)
    {
        if (!bypassLimit && Resolve(effectTargetUid, ref effectTargetComponent, false))
            if (effectTargetComponent.BehaviorProperties.BluespaceEffectMinInterval > 0)
            {
                var curTimeTicks = _timing.CurTick.Value;
                if (curTimeTicks < effectTargetComponent.BluespaceEffectNextTime)
                    return;

                effectTargetComponent.BluespaceEffectNextTime = curTimeTicks + (uint) (_timing.TickRate * effectTargetComponent.BehaviorProperties.BluespaceEffectMinInterval);
            }

        Spawn(effectSourceComponent.BehaviorProperties.BluespaceEffectPrototype, effectTargetUid.ToCoordinates());
    }

    private void PreOpen(EntityUid uid, BluespaceLockerComponent component, ref StorageBeforeOpenEvent args)
    {
        EntityStorageComponent? entityStorageComponent = null;
        int transportedEntities = 0;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        if (!component.BehaviorProperties.ActOnOpen)
            return;

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
                    if (EntityManager.HasComponent<MindContainerComponent>(entity))
                    {
                        if (!component.BehaviorProperties.TransportSentient)
                            continue;

                        _containerSystem.Insert(entity, entityStorageComponent.Contents);
                        transportedEntities++;
                    }
                    else if (component.BehaviorProperties.TransportEntities)
                    {
                        _containerSystem.Insert(entity, entityStorageComponent.Contents);
                        transportedEntities++;
                    }
                }

            // Move contained air
            if (component.BehaviorProperties.TransportGas)
            {
                entityStorageComponent.Air.CopyFrom(target.Value.storageComponent.Air);
                target.Value.storageComponent.Air.Clear();
            }

            // Bluespace effects
            if (component.BehaviorProperties.BluespaceEffectOnTeleportSource)
                BluespaceEffect(target.Value.uid, component, target.Value.bluespaceLockerComponent);
            if (component.BehaviorProperties.BluespaceEffectOnTeleportTarget)
                BluespaceEffect(uid, component, component);
        }

        DestroyAfterLimit(uid, component, transportedEntities);
    }

    private bool ValidLink(EntityUid locker, EntityUid link, BluespaceLockerComponent lockerComponent, bool intendToLink = false)
    {
        if (!link.Valid ||
            !TryComp<EntityStorageComponent>(link, out var linkStorage) ||
            linkStorage.LifeStage == ComponentLifeStage.Deleted ||
            link == locker)
            return false;

        if (lockerComponent.BehaviorProperties.InvalidateOneWayLinks &&
            !(intendToLink && lockerComponent.AutoLinksBidirectional) &&
            !(HasComp<BluespaceLockerComponent>(link) && Comp<BluespaceLockerComponent>(link).BluespaceLinks.Contains(locker)))
            return false;

        return true;
    }

    /// <returns>True if any HashSet in <paramref name="a"/> would grant access to <paramref name="b"/></returns>
    private bool AccessMatch(IReadOnlyCollection<HashSet<ProtoId<AccessLevelPrototype>>>? a, IReadOnlyCollection<HashSet<ProtoId<AccessLevelPrototype>>>? b)
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
        if (!ValidLink(locker, link, lockerComponent, true))
            return false;

        if (lockerComponent.PickLinksFromSameMap &&
            _transformSystem.GetMapId(link.ToCoordinates()) != _transformSystem.GetMapId(locker.ToCoordinates()))
            return false;

        if (lockerComponent.PickLinksFromStationGrids &&
            !HasComp<StationMemberComponent>(_transformSystem.GetGrid(link.ToCoordinates())))
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

    public (EntityUid uid, EntityStorageComponent storageComponent, BluespaceLockerComponent? bluespaceLockerComponent)? GetTarget(EntityUid lockerUid, BluespaceLockerComponent component, bool init = false)
    {
        while (true)
        {
            // Ensure MinBluespaceLinks
            if (component.BluespaceLinks.Count < component.MinBluespaceLinks)
            {
                // Get an shuffle the list of all EntityStorages
                var storages = new List<Entity<EntityStorageComponent>>();
                var query = EntityQueryEnumerator<EntityStorageComponent>();
                while (query.MoveNext(out var uid, out var storage))
                {
                    storages.Add((uid, storage));
                }

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
                        var targetBluespaceComponent = CompOrNull<BluespaceLockerComponent>(potentialLink);

                        if (targetBluespaceComponent == null)
                        {
                            targetBluespaceComponent = AddComp<BluespaceLockerComponent>(potentialLink);

                            if (component.AutoLinksBidirectional)
                                targetBluespaceComponent.BluespaceLinks.Add(lockerUid);

                            if (component.AutoLinksUseProperties)
                                targetBluespaceComponent.BehaviorProperties = component.AutoLinkProperties with {};

                            GetTarget(potentialLink, targetBluespaceComponent, true);
                            BluespaceEffect(potentialLink, targetBluespaceComponent, targetBluespaceComponent, true);
                        }
                        else if (component.AutoLinksBidirectional)
                        {
                            targetBluespaceComponent.BluespaceLinks.Add(lockerUid);
                        }
                    }
                    if (component.BluespaceLinks.Count >= component.MinBluespaceLinks)
                        break;
                }
            }

            // If there are no possible link targets and no links, return null
            if (component.BluespaceLinks.Count == 0)
            {
                if (component.MinBluespaceLinks == 0 && !init)
                    RemComp<BluespaceLockerComponent>(lockerUid);

                return null;
            }

            // Attempt to select, validate, and return a link
            var links = component.BluespaceLinks.ToArray();
            var link = links[_robustRandom.Next(0, component.BluespaceLinks.Count)];
            if (ValidLink(lockerUid, link, component))
                return (link, Comp<EntityStorageComponent>(link), CompOrNull<BluespaceLockerComponent>(link));
            component.BluespaceLinks.Remove(link);
        }
    }

    private void PostClose(EntityUid uid, BluespaceLockerComponent component, ref StorageAfterCloseEvent args)
    {
        PostClose(uid, component);
    }

    private void OnDoAfter(EntityUid uid, BluespaceLockerComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        PostClose(uid, component, false);

        args.Handled = true;
    }

    private void PostClose(EntityUid uid, BluespaceLockerComponent component, bool doDelay = true)
    {
        EntityStorageComponent? entityStorageComponent = null;
        int transportedEntities = 0;

        if (!Resolve(uid, ref entityStorageComponent))
            return;

        if (!component.BehaviorProperties.ActOnClose)
            return;

        // Do delay
        if (doDelay && component.BehaviorProperties.Delay > 0)
        {
            EnsureComp<DoAfterComponent>(uid);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.BehaviorProperties.Delay, new BluespaceLockerDoAfterEvent(), uid));
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
                if (EntityManager.HasComponent<MindContainerComponent>(entity))
                {
                    if (!component.BehaviorProperties.TransportSentient)
                        continue;

                    _containerSystem.Insert(entity, target.Value.storageComponent.Contents);
                    transportedEntities++;
                }
                else if (component.BehaviorProperties.TransportEntities)
                {
                    _containerSystem.Insert(entity, target.Value.storageComponent.Contents);
                    transportedEntities++;
                }
            }

        // Move contained air
        if (component.BehaviorProperties.TransportGas)
        {
            target.Value.storageComponent.Air.CopyFrom(entityStorageComponent.Air);
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
            if (_weldableSystem.IsWelded(target.Value.uid))
            {
                // It gets bluespaced open...
                _weldableSystem.SetWeldedState(target.Value.uid, false);
            }

            LockComponent? lockComponent = null;
            if (Resolve(target.Value.uid, ref lockComponent, false) && lockComponent.Locked)
                _lockSystem.Unlock(target.Value.uid, target.Value.uid, lockComponent);

            _entityStorage.OpenStorage(target.Value.uid, target.Value.storageComponent);
        }

        // Bluespace effects
        if (component.BehaviorProperties.BluespaceEffectOnTeleportSource)
            BluespaceEffect(uid, component, component);
        if (component.BehaviorProperties.BluespaceEffectOnTeleportTarget)
            BluespaceEffect(target.Value.uid, component, target.Value.bluespaceLockerComponent);

        DestroyAfterLimit(uid, component, transportedEntities);
    }

    private void DestroyAfterLimit(EntityUid uid, BluespaceLockerComponent component, int transportedEntities)
    {
        if (component.BehaviorProperties.DestroyAfterUsesMinItemsToCountUse > transportedEntities)
            return;

        if (component.BehaviorProperties.ClearLinksEvery != -1)
        {
            component.UsesSinceLinkClear++;
            if (component.BehaviorProperties.ClearLinksEvery <= component.UsesSinceLinkClear)
            {
                if (component.BehaviorProperties.ClearLinksDebluespaces)
                    foreach (var link in component.BluespaceLinks)
                        RemComp<BluespaceLockerComponent>(link);

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
                _explosionSystem.QueueExplosion(_transformSystem.ToMapCoordinates(uid.ToCoordinates()),
                    ExplosionSystem.DefaultExplosionPrototypeId, 4, 1, 2, uid, maxTileBreak: 0);
                goto case BluespaceLockerDestroyType.Delete;
            case BluespaceLockerDestroyType.Delete:
                QueueDel(uid);
                break;
            default:
            case BluespaceLockerDestroyType.DeleteComponent:
                RemComp<BluespaceLockerComponent>(uid);
                break;
        }
    }
}

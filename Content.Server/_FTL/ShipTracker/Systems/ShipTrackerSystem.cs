using System.Linq;
using Content.Server._FTL.AutomatedShip.Components;
using Content.Server._FTL.FTLPoints.Components;
using Content.Server._FTL.FTLPoints.Systems;
using Content.Server._FTL.ShipTracker.Events;
using Content.Server._FTL.Weapons;
using Content.Server.AlertLevel;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Systems;
using Content.Shared._FTL.ShipTracker;
using Content.Shared.Examine;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._FTL.ShipTracker.Systems;

/// <summary>
/// This handles tracking ships, healths and more
/// </summary>
public sealed partial class ShipTrackerSystem : SharedShipTrackerSystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FTLPointsSystem _pointsSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipTrackerComponent, FTLCompletedEvent>(OnFTLCompletedEvent);
        SubscribeLocalEvent<ShipTrackerComponent, FTLStartedEvent>(OnFTLStartedEvent);
        SubscribeLocalEvent<ShipTrackerComponent, FTLRequestEvent>(OnFTLRequestEvent);
        SubscribeLocalEvent<ShieldGeneratorComponent, ExaminedEvent>(OnShieldsExamined);

        SubscribeLocalEvent<RepairMainShipOnInitComponent, MapInitEvent>(OnRepairShipMapInit);

        SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
    }

    private void OnFTLRequestEvent(EntityUid uid, ShipTrackerComponent component, ref FTLRequestEvent args)
    {
        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ship-ftl-jump-jumped-message"), colorOverride: Color.Gold);
    }

    private void OnRepairShipMapInit(EntityUid uid, RepairMainShipOnInitComponent component, MapInitEvent args)
    {
        var ships = EntityQueryEnumerator<ShipTrackerComponent>();
        while (ships.MoveNext(out var ship, out var comp))
        {
            var transform = Transform(ship);
            if (transform.MapID == Transform(uid).MapID)
            {
                comp.HullAmount = comp.HullCapacity;
                _popupSystem.PopupCoordinates(Loc.GetString("repaired-popup-message"), Transform(uid).Coordinates);
                QueueDel(uid);
            }
        }
    }

    private void OnGridAdd(GridAddEvent msg, EntitySessionEventArgs args)
    {
        // icky
        EnsureComp<ShipTrackerComponent>(msg.EntityUid);
        EnsureComp<NavMapComponent>(msg.EntityUid);
    }

    private void OnFTLStartedEvent(EntityUid uid, ShipTrackerComponent component, ref FTLStartedEvent args)
    {
        if (args.FromMapUid != null)
            Del(args.FromMapUid.Value);

        _chatSystem.DispatchStationAnnouncement(uid, Loc.GetString("ship-ftl-jump-jumped-message"), colorOverride: Color.Gold);
    }

    private void OnFTLCompletedEvent(EntityUid uid, ShipTrackerComponent component, ref FTLCompletedEvent args)
    {
        RemComp<DisposalFTLPointComponent>(args.MapUid);

        var mapId = Transform(args.MapUid).MapID;
        _mapManager.DoMapInitialize(mapId);

        var amount = EntityQuery<AutomatedShipComponent>().Select(x => Transform(x.Owner).MapID == mapId).Count();
        if (amount > 0)
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ship-inbound-message", ("amount", amount)));
            _alertLevelSystem.SetLevel(args.Entity, "blue", true, true, true);
        }
        else
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ship-ftl-jump-arrival-message"),
                colorOverride: Color.Gold);
        }
        _pointsSystem.RegeneratePoints();
    }

    /// <summary>
    /// Attempts to damage the ship.
    /// </summary>
    /// <param name="target">The grid entity ID being targeted</param>
    /// <param name="prototype">The ammo prototype used for damage</param>
    /// <param name="ship">The ship tracker component of the target</param>
    /// <param name="source">The grid entity ID that caused the attack, used for hit tracking purposes</param>
    /// <returns>Whether the ship's *hull* was damaged. Returns false if it hit shields or didn't hit at all.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool TryDamageShip(EntityUid target, FTLAmmoType prototype, ShipTrackerComponent? ship = null, EntityUid? source = null)
    {
        if (!Resolve(target, ref ship))
            return false;

        var hit = false;
        if (_random.Prob(ship.PassiveEvasion))
            return false;

        if (!TryGetShieldHealth(target, out var shieldAmount, out _, ship))
            return false;

        for (var i = 0; i < prototype.HitTimes; i++)
        {
            ship.TimeSinceLastAttack = 0f;
            if ((prototype.NoShields && shieldAmount <= 0) || !prototype.NoShields)
            {
                if (shieldAmount <= 0 || prototype.ShieldPiercing)
                {
                    // damage hull
                    ship.HullAmount -= _random.Next(prototype.HullDamageMin, prototype.HullDamageMax);
                    hit = true;
                    continue;
                }
            }

            TryDamageShieldHealth(target, 1, ship);
            ship.TimeSinceLastShieldRegen = 0f; // reset the shield timer
        }

        if (!source.HasValue)
            return hit;

        var ev = new ShipDamagedEvent(source.Value, ship);
        RaiseLocalEvent(target, ref ev);

        return hit;
    }

    /// <summary>
    /// Attempts to damage the ship.
    /// </summary>
    /// <param name="targetGrid">The target grid</param>
    /// <param name="prototype">The damage prototype</param>
    /// <param name="source">The source grid, used for hit tracking</param>
    /// <returns>Whether the ship's *hull* was damaged. Returns false if it hit shields or didn't hit at all.</returns>
    public bool TryDamageShip(EntityUid targetGrid, FTLAmmoType prototype, EntityUid? source = null)
    {
        return TryComp<ShipTrackerComponent>(targetGrid, out var tracker) && TryDamageShip(targetGrid, prototype, tracker, source);
    }

    public bool TryFindRandomTile(EntityUid targetGrid, out Vector2i tile, out EntityCoordinates targetCoords)
    {
        tile = default;

        targetCoords = EntityCoordinates.Invalid;

        if (!TryComp<MapGridComponent>(targetGrid, out var gridComp))
            return false;

        var found = false;
        var (gridPos, _, gridMatrix) = _transformSystem.GetWorldPositionRotationMatrix(targetGrid);
        var gridBounds = gridMatrix.TransformBox(gridComp.LocalAABB);

        for (var i = 0; i < 10; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            if (_atmosphereSystem.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile,
                    mapGridComp: gridComp)
                || _atmosphereSystem.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp))
            {
                continue;
            }

            found = true;
            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        return found;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shipTrackerQuery = EntityQueryEnumerator<ShipTrackerComponent>();
        while (shipTrackerQuery.MoveNext(out var entity, out var comp))
        {
            comp.TimeSinceLastAttack += frameTime;
            comp.TimeSinceLastShieldRegen += frameTime;

            if (!TryGetShieldHealth(entity, out var shieldAmount, out var shieldCapacity, comp))
                continue;

            if (comp.TimeSinceLastShieldRegen >= comp.ShieldRegenTime && shieldAmount < shieldCapacity)
            {
                TryRegenerateShieldHealth(entity, 1, comp);
                comp.TimeSinceLastShieldRegen = 0f;
            }

            if (comp.HullAmount <= 0)
            {
                EnsureComp<FTLActiveShipDestructionComponent>(entity);
            }
        }

        var query = EntityQueryEnumerator<FTLActiveShipDestructionComponent>();
        while (query.MoveNext(out var entity, out var component))
        {
            if (!TryComp<ShipTrackerComponent>(entity, out var shipTracker))
            {
                continue;
            }

            if (shipTracker.HullAmount > 0)
            {
                _entityManager.RemoveComponent<FTLActiveShipDestructionComponent>(entity);
                continue;
            }

            var destroyAttempt = new ShipDestroyAttempt(shipTracker);
            RaiseLocalEvent(entity, ref destroyAttempt);

            if (destroyAttempt.Cancelled)
            {
                _entityManager.RemoveComponent<FTLActiveShipDestructionComponent>(entity);
                shipTracker.HullAmount += 1;
                continue;
            }

            var destroyBefore = new BeforeShipDestroy(shipTracker);
            RaiseLocalEvent(entity, ref destroyBefore);

            component.TimePassed += frameTime;
            component.TimePassedSinceLastExplosion += frameTime;

            if (component.TimePassedSinceLastExplosion >= component.ShipExplosionLimit)
            {
                if (!TryFindRandomTile(entity, out _, out var coordinates))
                {
                    Log.Error("Unable to find a tile!");
                }

                component.TimePassedSinceLastExplosion = 0f;
                _explosionSystem.QueueExplosion(coordinates.ToMap(_entityManager, _transformSystem), "Default", 2500, 15, 100);
            }

            if (!(component.TimePassed >= component.ShipDestructionLimit))
                continue;

            // should really just make this an event...
            _explosionSystem.QueueExplosion(entity, "Default", 500000, 15, 100);
            _entityManager.RemoveComponent<ShipTrackerComponent>(entity);
            _entityManager.RemoveComponent<FTLActiveShipDestructionComponent>(entity);
            _entityManager.RemoveComponent<AutomatedShipComponent>(entity);
            _entityManager.RemoveComponent<ActiveAutomatedShipComponent>(entity);

            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ship-destroyed-message",
                ("ship", MetaData(entity).EntityName)));

            var destroyAfter = new AfterShipDestroy(shipTracker);
            RaiseLocalEvent(entity, ref destroyAfter);
        }
    }
}

using System.Linq;
using Content.Server._FTL.AutomatedCombat;
using Content.Server._FTL.FTLPoints;
using Content.Server._FTL.ShipTracker.Events;
using Content.Server._FTL.Weapons;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking.Events;
using Content.Server.Popups;
using Content.Server.Shuttles.Events;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._FTL.ShipHealth;

/// <summary>
/// This handles tracking ships, healths and more
/// </summary>
public sealed class ShipTrackerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly FTLPointsSystem _pointsSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipTrackerComponent, FTLCompletedEvent>(OnFTLCompletedEvent);
        SubscribeLocalEvent<ShipTrackerComponent, FTLStartedEvent>(OnFTLStartedEvent);
        SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
        SubscribeLocalEvent<ShipTrackerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RepairMainShipOnInitComponent, MapInitEvent>(OnRepairShipMapInit);
    }

    private void OnRepairShipMapInit(EntityUid uid, RepairMainShipOnInitComponent component, MapInitEvent args)
    {
        var ships = EntityQueryEnumerator<MainCharacterShipComponent>();
        while (ships.MoveNext(out var ship, out _))
        {
            if (TryComp<ShipTrackerComponent>(ship, out var comp))
            {
                comp.HullAmount = comp.HullCapacity;
                _popupSystem.PopupCoordinates(Loc.GetString("repaired-popup-message"), Transform(uid).Coordinates);
                QueueDel(uid);
            }
        }
    }

    private void OnMapInit(EntityUid uid, ShipTrackerComponent component, MapInitEvent args)
    {
        _pointsSystem.RegeneratePoints();
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
    }

    private void OnFTLCompletedEvent(EntityUid uid, ShipTrackerComponent component, ref FTLCompletedEvent args)
    {
        RemComp<DisposalFTLPointComponent>(args.MapUid);

        var mapId = Transform(args.MapUid).MapID;
        _mapManager.DoMapInitialize(mapId);

        _pointsSystem.RegeneratePoints();
    }

    /// <summary>
    /// Attempts to damage the ship.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="prototype"></param>
    /// <returns>Whether the ship's *hull* was damaged. Returns false if it hit shields or didn't hit at all.</returns>
    public bool TryDamageShip(ShipTrackerComponent ship, FTLAmmoType prototype)
    {
        var hit = false;
        if (_random.Prob(ship.PassiveEvasion))
            return false;

        for (var i = 0; i < prototype.HitTimes; i++)
        {
            ship.TimeSinceLastAttack = 0f;
            if ((prototype.NoShields && ship.ShieldAmount <= 0) || !prototype.NoShields)
            {
                if (ship.ShieldAmount <= 0 || prototype.ShieldPiercing)
                {
                    // damage hull
                    ship.HullAmount -= _random.Next(prototype.HullDamageMin, prototype.HullDamageMax);
                    hit = true;
                    continue;
                }
            }
            ship.ShieldAmount--;
            ship.TimeSinceLastShieldRegen = 0f; // reset the shield timer
        }

        return hit;
    }

    /// <summary>
    /// Attempts to damage the ship.
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="prototype"></param>
    /// <returns>Whether the ship's *hull* was damaged. Returns false if it hit shields or didn't hit at all.</returns>
    public bool TryDamageShip(EntityUid grid, FTLAmmoType prototype)
    {
        if (!TryComp<ShipTrackerComponent>(grid, out var tracker))
            return false;
        return TryDamageShip(tracker, prototype);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shipTrackerQuery = EntityQueryEnumerator <ShipTrackerComponent>();
        while (shipTrackerQuery.MoveNext(out var entity, out var comp))
        {

            comp.TimeSinceLastAttack += frameTime;
            comp.TimeSinceLastShieldRegen += frameTime;

            if (comp.TimeSinceLastShieldRegen >= comp.ShieldRegenTime && comp.ShieldAmount < comp.ShieldCapacity)
            {
                comp.ShieldAmount++;
                comp.TimeSinceLastShieldRegen = 0f;
            }

            if (comp.HullAmount <= 0)
            {
                EnsureComp<FTLActiveShipDestructionComponent>(comp.Owner);
            }
        }

        var query = EntityQueryEnumerator <FTLActiveShipDestructionComponent>();
        while (query.MoveNext(out var entity, out var comp))
        {
            Log.Debug(entity.ToString());

            if (!TryComp<ShipTrackerComponent>(entity, out var shipTracker))
            {
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

            // should really just make this an event...
            _explosionSystem.QueueExplosion(entity, "Default", 500000, 15, 100);
            _entityManager.RemoveComponent<FTLActiveShipDestructionComponent>(entity);
            _entityManager.RemoveComponent<ShipTrackerComponent>(entity);
            _entityManager.RemoveComponent<MainCharacterShipComponent>(entity);
            _entityManager.RemoveComponent<AutomatedCombatComponent>(entity);
            _entityManager.RemoveComponent<ActiveAutomatedCombatComponent>(entity);

            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ship-destroyed-message", ("ship", MetaData(entity).EntityName)));

            var destroyAfter = new AfterShipDestroy(shipTracker);
            RaiseLocalEvent(entity, ref destroyAfter);
        }
    }
}

using Content.Server._FTL.FTLPoints;
using Content.Server._FTL.Weapons;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Shuttles.Events;
using Robust.Shared.Random;

namespace Content.Server._FTL.ShipHealth;

/// <summary>
/// This handles...
/// </summary>
public sealed class FTLShipHealthSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ExplosionSystem _explosionSystem = default!;
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private FTLPointsSystem _pointsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FTLShipHealthComponent, FTLCompletedEvent>(OnFTLCompletedEvent);
        SubscribeLocalEvent<FTLShipHealthComponent, FTLStartedEvent>(OnFTLStartedEvent);
        SubscribeLocalEvent<FTLShipHealthComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, FTLShipHealthComponent component, ComponentInit args)
    {
        _pointsSystem.RegeneratePoints();
    }

    private void OnFTLStartedEvent(EntityUid uid, FTLShipHealthComponent component, ref FTLStartedEvent args)
    {
        if (args.FromMapUid != null)
            Del(args.FromMapUid.Value);
    }

    private void OnFTLCompletedEvent(EntityUid uid, FTLShipHealthComponent component, ref FTLCompletedEvent args)
    {
        RemComp<DisposalFTLPointComponent>(args.MapUid);
        _pointsSystem.RegeneratePoints();
    }

    /// <summary>
    /// Attempts to damage the ship.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="prototype"></param>
    /// <returns>Whether the ship's *hull* was damaged. Returns false if it hit shields or didn't hit at all.</returns>
    public bool TryDamageShip(FTLShipHealthComponent ship, FTLAmmoType prototype)
    {
        if (_random.Prob(ship.PassiveEvasion))
            return false;

        ship.TimeSinceLastAttack = 0f;
        if (ship.ShieldAmount <= 0 || prototype.ShieldPiercing)
        {
            // damage hull
            ship.HullAmount -= _random.Next(prototype.HullDamageMin, prototype.HullDamageMax);
            return true;
        }
        else
        {
            ship.ShieldAmount--;
            ship.TimeSinceLastShieldRegen = 5f;
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityManager.EntityQuery<FTLShipHealthComponent>())
        {
            comp.TimeSinceLastAttack += frameTime;
            comp.TimeSinceLastShieldRegen += frameTime;

            if (comp.TimeSinceLastShieldRegen >= comp.ShieldRegenTime)
            {
                comp.ShieldAmount++;
                comp.TimeSinceLastShieldRegen = 0f;
            }

            if (comp.HullAmount <= 0)
            {
                AddComp<FTLActiveShipDestructionComponent>(comp.Owner);
            }
        }

        foreach (var shipDestruction in EntityManager.EntityQuery<FTLActiveShipDestructionComponent>())
        {
            _explosionSystem.QueueExplosion(shipDestruction.Owner, "Default", 100000, 5, 100, 0f, 0, false);
            _entityManager.RemoveComponent<FTLActiveShipDestructionComponent>(shipDestruction.Owner);
            _entityManager.RemoveComponent<FTLShipHealthComponent>(shipDestruction.Owner);
        }
    }
}

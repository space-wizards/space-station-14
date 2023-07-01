using Content.Server._FTL.Weapons;
using Content.Server.Explosion.EntitySystems;
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
}

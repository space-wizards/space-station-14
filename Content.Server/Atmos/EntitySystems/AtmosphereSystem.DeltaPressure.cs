using System.Diagnostics;
using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Damage;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Processes a singular entity, determining the pressures it's experiencing and applying damage based on that.
    /// </summary>
    /// <param name="ent">The entity to process.</param>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/> that belongs to the entity's GridUid.</param>
    private void ProcessDeltaPressureEntity(Entity<DeltaPressureComponent> ent, GridAtmosphereComponent gridAtmosComp)
    {
        // Retrieve the current tile coords of this ent, use cached lookup
        // TODO: might be good to cache this alongside the stored ent in the hashset
        if (!_transformQuery.TryComp(ent, out var xform))
            return;

        // TODO: profile because doing this for every tile seems really bad
        var indices = _transformSystem.GetGridOrMapTilePosition(ent, xform);

        // TODO: probably can be null suppressed
        if (!gridAtmosComp.Tiles.TryGetValue(indices, out var tileAtmos))
        {
            return;
        }

        // Next, we need to check if this entity is airtight.
        // If it isn't, we can save a lot of work.
        if (!_airtightQuery.TryComp(ent, out var airtightComp))
        {
            return;
        }

        // Since we're not airtight, we can simply determine the pressure
        // acting on the entity, as we don't expect there to be any pressure deltas.
        // We cannot use the cached AirtightData that AtmosphereSystem collects as
        // this entity could be something like a soda can.
        if (!airtightComp.AirBlocked && tileAtmos.Air != null)
        {
            PerformDamage(ent, tileAtmos.Air.Pressure);
            if (ent.Comp.StackDamage)
            {
                return;
            }
        }

        /*
         We need to determine comparisons in a performant way.
         To generalize, we can simply compare the N - S and E - W directions.
         */

        // This effectively checks the N direction and the E direction
        // (as we check the opposing side at the same time
        var maxDeltaPressure = 0f;
        for (var i = 0; i < Atmospherics.Directions; i += 2)
        {
            var dir = (AtmosDirection)(1 << i);
            var oppDir = dir.GetOpposite();

            // gridAtmosComp.Tiles.TryGetValue(indices.Offset(direction), out var tile1);
            // gridAtmosComp.Tiles.TryGetValue(indices.Offset(direction.GetOpposite()), out var tile2);
            var tile1 = CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices.Offset(dir));
            var tile2 = CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices.Offset(oppDir));

            // If both TileAtmospheres are somehow null then there's no delta-P.
            var deltaPressure = 0f;

            if (tile1 is { Air: not null })
            {
                // If only one side has air, then we're bearing down on the window with full force.
                deltaPressure = tile1.Air.Pressure;

                // Both sides aren't null, so now we compute a proper delta-P.
                if (tile2 is { Air: not null })
                {
                    deltaPressure = Math.Abs(deltaPressure - tile2.Air.Pressure);
                }
            }

            maxDeltaPressure = Math.Max(deltaPressure, maxDeltaPressure);
        }

        if (maxDeltaPressure > ent.Comp.MinPressureDelta)
        {
            PerformDamage(ent, maxDeltaPressure);
        }
    }

    /// <summary>
    /// Does damage to an entity depending on the pressure experienced by it.
    /// </summary>
    /// <param name="ent">The entity to apply damage to.</param>
    /// <param name="pressure">The absolute pressure being exerted on the entity.</param>
    private void PerformDamage(Entity<DeltaPressureComponent> ent, float pressure)
    {
        var realPressure = Math.Max(pressure, ent.Comp.MaxPressure);
        var appliedDamage = ent.Comp.BaseDamage;
        switch (ent.Comp.ScalingType)
        {
            case DeltaPressureDamageScalingType.Threshold:
                break;

            case DeltaPressureDamageScalingType.Linear:
                appliedDamage *= realPressure * ent.Comp.ScalingPower;
                break;

            case DeltaPressureDamageScalingType.Log:
                // This little line's gonna cost us 51 CPU cycles
                // TODO: Harden this against NaN bullshit because I 100% expect it later
                appliedDamage *= Math.Log(realPressure, ent.Comp.ScalingPower);
                break;

            case DeltaPressureDamageScalingType.Exponential:
                appliedDamage *= Math.Pow(realPressure, ent.Comp.ScalingPower);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid damage scaling type!");
        }

        // TODO: this feels like ass
        _damage.TryChangeDamage(ent, appliedDamage, interruptsDoAfters: false);
        ent.Comp.IsTakingDamage = true;
    }
}

using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Shared.Threading;

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

        /*
         We need to determine comparisons in a performant way.
         To generalize, we can simply compare the N - S and E - W directions.
         */

        // First, we null check data and prep it for comparison.
        Span<float> floatArray = stackalloc float[4];
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var dir = (AtmosDirection)(1 << i);
            ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices.Offset(dir));

            // This can be a null ref! We need to check it or bad things will happen!
            if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref tile))
                floatArray[i] = tile.Air?.Pressure ?? 0f;
        }

        // This effectively checks the N direction and the E direction
        // (as we check the opposing side at the same time
        var maxDeltaPressure = 0f;
        for (var i = 0; i < Atmospherics.Directions; i += 2)
        {
            var oppi = i.ToOppositeIndex();
            var deltaPressure = Math.Abs(floatArray[i] - floatArray[oppi]);

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

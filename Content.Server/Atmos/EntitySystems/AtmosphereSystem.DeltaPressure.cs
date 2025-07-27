using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;

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
        if (!_transformQuery.TryComp(ent, out var xform))
            return;

        var indices = _transformSystem.GetGridOrMapTilePosition(ent, xform);

        /*
         To make our comparisons a little bit faster, we take advantage of SIMD-accelerated methods
         in the NumericsHelpers class.

         This involves loading our values into a span in the form of opposing pairs,
         so simple vector operations like min/max/abs can be performed on them.
         */

        // Directions are always in pairs: the number of directions is always even
        // (we must consider the future where Multi-Z is real)
        const int pairCount = Atmospherics.Directions / 2;

        Span<float> opposingGroupA = stackalloc float[pairCount]; // Will hold North, East, ...
        Span<float> opposingGroupB = stackalloc float[pairCount]; // Will hold South, West, ...

        // First, we null check data and prep it for comparison
        for (var i = 0; i < pairCount; i++)
        {
            // First direction in the pair (North, East, ...)
            var dirA = (AtmosDirection)(1 << i);

            // We can use GetValueRefOrNullRef here as we're not modifying the dictionary.
            // Supposed to be a bit faster than using TryGetValue.
            ref var tileA = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices.Offset(dirA));

            // This can be a null ref! We need to check it or bad things will happen!
            if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref tileA) && tileA.Air != null)
            {
                opposingGroupA[i] = tileA.Air.Pressure;
            }

            // Second direction in the pair (South, West, ...)
            var dirB = (AtmosDirection)(1 << (i + pairCount));
            ref var tileB = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices.Offset(dirB));

            if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref tileB) && tileB.Air != null)
            {
                opposingGroupA[i] = tileB.Air.Pressure;
            }
        }

        // Calculate pressure differences between opposing directions
        NumericsHelpers.Sub(opposingGroupA, opposingGroupB);
        NumericsHelpers.Abs(opposingGroupA);

        // Find maximum pressure difference
        var maxDelta = 0f;
        for (var i = 0; i < pairCount; i++)
        {
            if (opposingGroupA[i] > maxDelta)
                maxDelta = opposingGroupA[i];
        }

        if (maxDelta > ent.Comp.MinPressureDelta)
        {
            PerformDamage(ent, maxDelta);
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
                appliedDamage *= Math.Log(realPressure, ent.Comp.ScalingPower);
                break;

            case DeltaPressureDamageScalingType.Exponential:
                appliedDamage *= Math.Pow(realPressure, ent.Comp.ScalingPower);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid damage scaling type!");
        }

        _damage.TryChangeDamage(ent, appliedDamage, interruptsDoAfters: false);
        ent.Comp.IsTakingDamage = true;
    }
}

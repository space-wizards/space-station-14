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
        // Retrieve the current tile coords of this ent, use cached lookup.
        // This ent could also just not exist anymore when we finally got around to processing it
        // (as atmos spans processing across multiple ticks), so this is a good check for that.
        if (!TryComp(ent, out TransformComponent? xform))
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
        Span<float> opposingGroupMax = stackalloc float[pairCount];

        // First, we null check data and prep it for comparison
        for (var i = 0; i < pairCount; i++)
        {
            // First direction in the pair (North, East, ...)
            var dirA = (AtmosDirection)(1 << i);

            // Second direction in the pair (South, West, ...)
            var dirB = (AtmosDirection)(1 << (i + pairCount));

            opposingGroupA[i] = GetTilePressure(gridAtmosComp, indices.Offset(dirA));
            opposingGroupB[i] = GetTilePressure(gridAtmosComp, indices.Offset(dirB));
        }

        // Need to determine max pressure in opposing directions.
        NumericsHelpers.Max(opposingGroupA, opposingGroupB, opposingGroupMax);

        // Calculate pressure differences between opposing directions.
        NumericsHelpers.Sub(opposingGroupA, opposingGroupB);
        NumericsHelpers.Abs(opposingGroupA);

        var maxPressure = 0f;
        for (var i = 0; i < pairCount; i++)
        {
            maxPressure = Math.Max(maxPressure, opposingGroupMax[i]);
        }

        // Find maximum pressure difference
        var maxDelta = 0f;
        for (var i = 0; i < pairCount; i++)
        {
            maxDelta = Math.Max(maxDelta, opposingGroupA[i]);
        }

        PerformDamage(ent, maxPressure, maxDelta);
    }

    // TODO: Move to API
    /// <summary>
    /// Does damage to an entity depending on the pressure experienced by it, based on the
    /// entity's <see cref="DeltaPressureComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to apply damage to.</param>
    /// <param name="pressure">The absolute pressure being exerted on the entity.</param>
    /// <param name="deltaPressure">The delta pressure being exerted on the entity.</param>
    private void PerformDamage(Entity<DeltaPressureComponent> ent, float pressure, float deltaPressure)
    {
        var aboveMinPressure = pressure > ent.Comp.MinPressure;
        var aboveMinDeltaPressure = deltaPressure > ent.Comp.MinPressureDelta;
        if (!aboveMinPressure && !aboveMinDeltaPressure)
        {
            ent.Comp.IsTakingDamage = false;
            return;
        }

        // shitcode
        var appliedDamage = ent.Comp.BaseDamage;
        if (aboveMinPressure)
        {
            appliedDamage = MutateDamage(ent, appliedDamage, pressure - ent.Comp.MinPressure);
        }
        if (aboveMinDeltaPressure)
        {
            if (ent.Comp.StackDamage)
            {
                appliedDamage += MutateDamage(ent, appliedDamage, deltaPressure - ent.Comp.MinPressureDelta);
            }
            else
            {
                appliedDamage = MutateDamage(ent, appliedDamage, deltaPressure - ent.Comp.MinPressureDelta);
            }
        }

        _damage.TryChangeDamage(ent, appliedDamage, interruptsDoAfters: false);
        ent.Comp.IsTakingDamage = true;
    }

    // TODO: Move to API
    /// <summary>
    /// Mutates the damage dealt by a DamageSpecifier based on current entity conditions and pressure.
    /// </summary>
    /// <param name="ent">The entity to base the manipulations off of (pull scaling type)</param>
    /// <param name="damage">The damage specifier to mutate.</param>
    /// <param name="pressure">The pressure being exerted on the entity.</param>
    /// <returns></returns>
    private DamageSpecifier MutateDamage(Entity<DeltaPressureComponent> ent, DamageSpecifier damage, float pressure)
    {
        switch (ent.Comp.ScalingType)
        {
            case DeltaPressureDamageScalingType.Threshold:
                break;

            case DeltaPressureDamageScalingType.Linear:
                damage *= pressure * ent.Comp.ScalingPower;
                break;

            case DeltaPressureDamageScalingType.Log:
                // This little line's gonna cost us 51 CPU cycles
                damage *= Math.Log(pressure, ent.Comp.ScalingPower);
                break;

            case DeltaPressureDamageScalingType.Exponential:
                damage *= Math.Pow(pressure, ent.Comp.ScalingPower);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid damage scaling type!");
        }

        return damage;
    }

    /// <summary>
    /// Retrieves a cached lookup of the pressure at a specific tile index on a grid.
    /// If not found, caches the pressure value for that tile index.
    /// </summary>
    /// <param name="gridAtmosComp">The grid to check.</param>
    /// <param name="indices">The indices to check.</param>
    private float GetTilePressure(GridAtmosphereComponent gridAtmosComp, Vector2i indices)
    {
        // First try and retrieve the tile atmosphere for the given indices from our cache.
        // Use a safe lookup method because we're going to be writing to the dictionary.
        if (gridAtmosComp.DeltaPressureCoords.TryGetValue(indices, out var cf))
        {
            return cf;
        }

        // Didn't hit the cache.
        // Since we're not writing to this dict, we can use an unsafe lookup method.
        // Supposed to be a bit faster, though we need to check for null refs.
        ref var tileA = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices);
        var nf = 0f;

        if (!System.Runtime.CompilerServices.Unsafe.IsNullRef(ref tileA) && tileA.Air != null)
        {
            // Cache the pressure value for this tile index.
            nf = tileA.Air.Pressure;
        }

        gridAtmosComp.DeltaPressureCoords[indices] = nf;
        return nf;
    }
}

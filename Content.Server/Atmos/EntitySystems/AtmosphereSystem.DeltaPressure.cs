using System.Diagnostics;
using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using static System.Runtime.CompilerServices.Unsafe;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// The number of pairs of opposing directions we can have.
    /// This is Atmospherics.Directions / 2, since we always compare opposing directions
    /// (e.g. North vs South, East vs West, etc.).
    /// Used to determine the size of the opposing groups when processing delta pressure entities.
    /// </summary>
    private const int DeltaPressurePairCount = Atmospherics.Directions / 2;

    /// <summary>
    /// Processes a singular entity, determining the pressures it's experiencing and applying damage based on that.
    /// </summary>
    /// <param name="ent">The entity to process.</param>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/> that belongs to the entity's GridUid.</param>
    /// <param name="opposingGroupA">Span containing the pressures in one set of opposing directions for comparison.</param>
    /// <param name="opposingGroupB">Span containing the pressures in the opposite set of directions to <paramref name="opposingGroupA"/>.</param>
    /// <param name="opposingGroupMax">Span to store the maximum pressures between each pair of opposing directions.</param>
    private void ProcessDeltaPressureEntity(Entity<DeltaPressureComponent> ent,
        GridAtmosphereComponent gridAtmosComp,
        Span<float> opposingGroupA,
        Span<float> opposingGroupB,
        Span<float> opposingGroupMax)
    {
        // These should be of length Atmospherics.Directions / 2, so we can use them to compare opposing directions.
        // If not, then someone messed up somehow.
        Debug.Assert(opposingGroupA.Length == DeltaPressurePairCount);
        Debug.Assert(opposingGroupB.Length == DeltaPressurePairCount);
        Debug.Assert(opposingGroupMax.Length == DeltaPressurePairCount);

        if (ent.Comp.RandomDamageChance is not 1f &&
            _random.NextFloat() >= ent.Comp.RandomDamageChance)
        {
            return;
        }

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

        // First, we null check data and prep it for comparison
        for (var i = 0; i < DeltaPressurePairCount; i++)
        {
            // First direction in the pair (North, East, ...)
            var dirA = (AtmosDirection)(1 << i);

            // Second direction in the pair (South, West, ...)
            var dirB = (AtmosDirection)(1 << (i + DeltaPressurePairCount));

            opposingGroupA[i] = GetTilePressure(gridAtmosComp, indices.Offset(dirA));
            opposingGroupB[i] = GetTilePressure(gridAtmosComp, indices.Offset(dirB));
        }

        // Need to determine max pressure in opposing directions.
        NumericsHelpers.Max(opposingGroupA, opposingGroupB, opposingGroupMax);

        // Calculate pressure differences between opposing directions.
        NumericsHelpers.Sub(opposingGroupA, opposingGroupB);
        NumericsHelpers.Abs(opposingGroupA);

        var maxPressure = 0f;
        for (var i = 0; i < DeltaPressurePairCount; i++)
        {
            maxPressure = Math.Max(maxPressure, opposingGroupMax[i]);
        }

        // Find maximum pressure difference
        var maxDelta = 0f;
        for (var i = 0; i < DeltaPressurePairCount; i++)
        {
            maxDelta = Math.Max(maxDelta, opposingGroupA[i]);
        }

        _deltaPressure.PerformDamage(ent, maxPressure, maxDelta);
    }

    /// <summary>
    /// Retrieves a cached lookup of the pressure at a specific tile index on a grid.
    /// If not found, caches the pressure value for that tile index.
    /// </summary>
    /// <param name="gridAtmosComp">The grid to check.</param>
    /// <param name="indices">The indices to check.</param>
    private static float GetTilePressure(GridAtmosphereComponent gridAtmosComp, Vector2i indices)
    {
        // First try and retrieve the tile atmosphere for the given indices from our cache.
        // Use a safe lookup method because we're going to be writing to the dictionary.
        if (gridAtmosComp.DeltaPressureCache.TryGetValue(indices, out var cachedFloat))
        {
            return cachedFloat;
        }

        // Didn't hit the cache.
        // Since we're not writing to this dict, we can use an unsafe lookup method.
        // Supposed to be a bit faster, though we need to check for null refs.
        ref var tileA = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices);
        var newFloat = 0f;

        if (!IsNullRef(ref tileA) && tileA.Air != null)
        {
            // Cache the pressure value for this tile index.
            newFloat = tileA.Air.Pressure;
        }

        gridAtmosComp.DeltaPressureCache[indices] = newFloat;
        return newFloat;
    }
}

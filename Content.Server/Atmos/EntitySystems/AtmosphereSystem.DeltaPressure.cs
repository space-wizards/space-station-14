using System.Diagnostics;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Shared.Random;
using Robust.Shared.Threading;

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

    public readonly record struct DeltaPressureDamageResult(Entity<DeltaPressureComponent> Ent, float Pressure, float DeltaPressure);

    private void EnqueueDeltaPressureDamage(Entity<DeltaPressureComponent> ent,
        GridAtmosphereComponent gridAtmosComp,
        float pressure,
        float delta)
    {
        gridAtmosComp.DeltaPressureDamageResults.Enqueue(new DeltaPressureDamageResult(ent, pressure, delta));
    }

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

        // Need to use system prob instead of robust prob
        // for thread safety.
        if (!Random.Shared.Prob(ent.Comp.RandomDamageChance))
            return;

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
        var maxDelta = 0f;
        for (var i = 0; i < DeltaPressurePairCount; i++)
        {
            var curMax = opposingGroupMax[i];
            if (curMax > maxPressure)
                maxPressure = curMax;

            var curDelta = opposingGroupA[i];
            if (curDelta > maxDelta)
                maxDelta = curDelta;
        }

        EnqueueDeltaPressureDamage(ent,
            gridAtmosComp,
            maxPressure,
            maxDelta);
    }

    /// <summary>
    /// Retrieves a cached lookup of the pressure at a specific tile index on a grid.
    /// If not found, caches the pressure value for that tile index.
    /// </summary>
    /// <param name="gridAtmosComp">The grid to check.</param>
    /// <param name="indices">The indices to check.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetTilePressure(GridAtmosphereComponent gridAtmosComp, Vector2i indices)
    {
        return gridAtmosComp.Tiles.TryGetValue(indices, out var tile) && tile.Air != null ? tile.Air.Pressure : 0f;
    }

    private sealed class DeltaPressureParallelJob(
        AtmosphereSystem system,
        GridAtmosphereComponent atmosphere,
        int startIndex)
        : IParallelRobustJob
    {
        // Process entities one-by-one per batch element.
        public int BatchSize => 100;

        public void Execute(int index)
        {
            var actualIndex = startIndex + index;
            if (actualIndex < 0 || actualIndex >= atmosphere.DeltaPressureEntities.Count)
                return;

            var ent = atmosphere.DeltaPressureEntities[actualIndex];

            Span<float> opposingGroupA = stackalloc float[DeltaPressurePairCount];
            Span<float> opposingGroupB = stackalloc float[DeltaPressurePairCount];
            Span<float> opposingGroupMax = stackalloc float[DeltaPressurePairCount];

            system.ProcessDeltaPressureEntity(ent, atmosphere, opposingGroupA, opposingGroupB, opposingGroupMax);
        }
    }
}

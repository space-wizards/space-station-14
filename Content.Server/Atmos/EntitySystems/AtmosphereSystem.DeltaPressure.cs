using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    private static void EnqueueDeltaPressureDamage(Entity<DeltaPressureComponent> ent,
        GridAtmosphereComponent gridAtmosComp,
        float pressure,
        float delta)
    {
        var aboveMinPressure = pressure > ent.Comp.MinPressure;
        var aboveMinDeltaPressure = delta > ent.Comp.MinPressureDelta;
        if (!aboveMinPressure && !aboveMinDeltaPressure)
        {
            ent.Comp.IsTakingDamage = false;
            return;
        }

        gridAtmosComp.DeltaPressureDamageResults.Enqueue(new DeltaPressureDamageResult(ent,
            pressure,
            delta,
            aboveMinPressure,
            aboveMinDeltaPressure));
    }

    /// <summary>
    /// Processes a singular entity, determining the pressures it's experiencing and applying damage based on that.
    /// </summary>
    /// <param name="ent">The entity to process.</param>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/> that belongs to the entity's GridUid.</param>
    private void ProcessDeltaPressureEntity(Entity<DeltaPressureComponent> ent,
        GridAtmosphereComponent gridAtmosComp)
    {
        // Need to use system prob instead of robust prob
        // for thread safety.
        if (!_random.Prob(ent.Comp.RandomDamageChance))
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
        Span<float> opposingGroupA = stackalloc float[DeltaPressurePairCount];
        Span<float> opposingGroupB = stackalloc float[DeltaPressurePairCount];
        Span<float> opposingGroupMax = stackalloc float[DeltaPressurePairCount];

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
            maxPressure = MathF.Max(maxPressure, opposingGroupMax[i]);
            maxDelta = MathF.Max(maxDelta, opposingGroupA[i]);
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
    private static float GetTilePressure(GridAtmosphereComponent gridAtmosComp, Vector2i indices)
    {
        // Since we're not writing to this dict, we can use an unsafe method to get the value.
        // Was ever so slightly faster than using TryGetValue.
        ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices);
        if (Unsafe.IsNullRef(ref tile))
            return 0f;

        return tile.Air?.Pressure ?? 0f;
    }

    /// <summary>
    /// Job for solving DeltaPressure entities in parallel.
    /// Batches are given some index to start from, so each thread can simply just start at that index
    /// and process the next n entities in the list.
    /// </summary>
    private sealed class DeltaPressureParallelJob(
        AtmosphereSystem system,
        GridAtmosphereComponent atmosphere,
        int startIndex)
        : IParallelRobustJob
    {
        public int BatchSize => 100;

        public void Execute(int index)
        {
            // The index is relative to the startIndex (because we can pause and resume computation),
            // so we need to add it to the startIndex.
            var actualIndex = startIndex + index;

            // Index can occasionally be out of bounds. :)
            if (actualIndex < 0 || actualIndex >= atmosphere.DeltaPressureEntities.Count)
                return;

            var ent = atmosphere.DeltaPressureEntities[actualIndex];
            system.ProcessDeltaPressureEntity(ent, atmosphere);
        }
    }

    /// <summary>
    /// Struct that holds the result of delta pressure damage processing for an entity.
    /// This is only created and enqueued when the entity needs to take damage.
    /// </summary>
    /// <param name="Ent"></param>
    /// <param name="Pressure"></param>
    /// <param name="DeltaPressure"></param>
    /// <param name="AboveMinPressure"></param>
    /// <param name="AboveMinDeltaPressure"></param>
    public readonly record struct DeltaPressureDamageResult(
        Entity<DeltaPressureComponent> Ent,
        float Pressure,
        float DeltaPressure,
        bool AboveMinPressure = false,
        bool AboveMinDeltaPressure = false);
}

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Threading;
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
    /// Processes a list of entities, determining the pressures it's experiencing and applying damage based on that.
    /// </summary>
    private void ProcessDeltaPressureEntities(GridAtmosphereComponent gridAtmosComp, int toProcess)
    {
        var job = new DeltaPressureJob
        {
            System = this,
            CurrentRunDeltaPressureEntities = gridAtmosComp.CurrentRunDeltaPressureEntities,
            GridAtmosComp = gridAtmosComp,
        };

        _parallel.ProcessNow(job, toProcess);
    }

    /// <summary>
    /// Retrieves the pressure at a specific tile index on a grid.
    /// </summary>
    /// <param name="gridAtmosComp">The grid to check.</param>
    /// <param name="indices">The indices to check.</param>
    private float GetTilePressure(GridAtmosphereComponent gridAtmosComp, Vector2i indices)
    {
        ref var tileA = ref CollectionsMarshal.GetValueRefOrNullRef(gridAtmosComp.Tiles, indices);
        var newFloat = 0f;

        if (!IsNullRef(ref tileA) && tileA.Air != null)
        {
            // Cache the pressure value for this tile index.
            newFloat = tileA.Air.Pressure;
        }

        return newFloat;
    }

    /// <summary>
    /// Job that computes the delta pressure for all entities with a <see cref="DeltaPressureComponent"/>.
    /// </summary>
    [UsedImplicitly]
    private readonly record struct DeltaPressureJob : IParallelRobustJob
    {
        public int BatchSize => 10;
        public required AtmosphereSystem System { get; init; }
        public required ConcurrentQueue<Entity<DeltaPressureComponent>> CurrentRunDeltaPressureEntities { get; init; }
        public required GridAtmosphereComponent GridAtmosComp { get; init; }

        public void Execute(int index)
        {
            CurrentRunDeltaPressureEntities.TryDequeue(out var ent);

            // Retrieve the current tile coords of this ent
            if (!System.TryComp(ent, out TransformComponent? xform))
                return;

            var indices = System._transformSystem.GetGridOrMapTilePosition(ent, xform);

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
            Span<float> opposingGroupMax = stackalloc float[DeltaPressurePairCount];

            // First, we null check data and prep it for comparison
            for (var i = 0; i < DeltaPressurePairCount; i++)
            {
                // First direction in the pair (North, East, ...)
                var dirA = (AtmosDirection)(1 << i);

                // Second direction in the pair (South, West, ...)
                var dirB = (AtmosDirection)(1 << (i + DeltaPressurePairCount));

                opposingGroupA[i] = System.GetTilePressure(GridAtmosComp, indices.Offset(dirA));
                opposingGroupB[i] = System.GetTilePressure(GridAtmosComp, indices.Offset(dirB));
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

            System._deltaPressure.PerformDamage(ent, maxPressure, maxDelta);
        }
    }
}

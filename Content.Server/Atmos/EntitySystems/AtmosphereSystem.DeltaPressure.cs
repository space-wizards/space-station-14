using System.Buffers;
using System.Collections.Concurrent;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Robust.Shared.Random;
using Robust.Shared.Threading;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// <para>A queue that handles scheduling of invalid entities to be removed from the entity processing list.</para>
    ///
    /// <para>We cannot change the contents of the list while processing it in parallel as this may create
    /// a race condition for other thread pool workers working on different parts of the same list (as removing
    /// items from the list will do a substitution of items to fill the gap, which can touch ents
    /// other threads may be working on).</para>
    ///
    /// <para>As such, we just delay removal of these entities until after parallel processing.</para>
    /// </summary>
    private readonly ConcurrentQueue<Entity<DeltaPressureComponent>> _deltaPressureInvalidEntityQueue = new();

    /// <summary>
    /// The number of pairs of opposing directions we can have.
    /// This is Atmospherics.Directions / 2, since we always compare opposing directions
    /// (e.g. North vs South, East vs West, etc.).
    /// Used to determine the size of the opposing groups when processing delta pressure entities.
    /// </summary>
    private const int DeltaPressurePairCount = Atmospherics.Directions / 2;

    /// <summary>
    /// The length to pre-allocate list/dicts of delta pressure entities on a <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    public const int DeltaPressurePreAllocateLength = 1000;

    /// <summary>
    /// Bulk processes a range of <see cref="DeltaPressureComponent"/> entities on a <see cref="GridAtmosphereComponent"/>
    /// from a starting index to an ending index,
    /// determining the pressures they're experiencing and applying damage based on that.
    /// </summary>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/> that belongs to the entity's GridUid.</param>
    /// <param name="start">The starting index in the DeltaPressureEntities list to process from.</param>
    /// <param name="end">The ending index in the DeltaPressureEntities list to process to.</param>
    private void ProcessDeltaPressureEntityBulk(GridAtmosphereComponent gridAtmosComp, int start, int end)
    {
        /*
         To make our comparisons a little bit faster, we take advantage of SIMD-accelerated methods.
         This requires that we load our data into spans to process them in bulk.

         This code takes advantage of ArrayPool so we can super easily reuse memory per tick
         in threading contexts, otherwise this will literally obliterate GC with a nuclear bomb.
         */

        var entList = gridAtmosComp.DeltaPressureEntities;
        var len = end - start;

        const int dirs = Atmospherics.Directions;
        // Total number of tiles to gather = number of entities * number of directions.
        var lenDirs = len * dirs;
        // Total number of opposing direction pairs to process = number of entities * number of opposing direction pairs.
        var pairLen = len * DeltaPressurePairCount;

        // Memory is meant to be used. Allocate a bunch of it.
        // Note that the arrays returned by ArrayPool are usually larger than requested
        // (ASK MY DEBUGGER HOW I KNOW) so we ask Span to give us a slice of the correct length.
        var tilesArr = ArrayPool<TileAtmosphere?>.Shared.Rent(lenDirs);
        var tiles = tilesArr.AsSpan(0, lenDirs);

        // I really hate dict lookups in hot loops so we keep AirtightComps on us.
        // This is the only array that isn't sliced since we do not have any operation that
        // requires SIMD on it, either directly or indirectly
        // (SIMD requires that arrays are same length or else indexes/supporting code will likely overstep).
        var airtightCompsArr = ArrayPool<AirtightComponent>.Shared.Rent(len);

        var groupAArr = ArrayPool<float>.Shared.Rent(pairLen);
        var groupBArr = ArrayPool<float>.Shared.Rent(pairLen);
        var groupMaxArr = ArrayPool<float>.Shared.Rent(pairLen);

        var groupA = groupAArr.AsSpan(0, pairLen);
        var groupB = groupBArr.AsSpan(0, pairLen);
        var groupMax = groupMaxArr.AsSpan(0, pairLen);

        var pressuresArr = ArrayPool<float>.Shared.Rent(lenDirs);
        var pressures = pressuresArr.AsSpan(0, lenDirs);

        try
        {
            // Gather tiles & airtight components.
            for (var i = 0; i < len; i++)
            {
                var ent = entList[start + i];

                // Ensure that the list still only contains valid airtight entities.
                // We cannot remove them here, so we enqueue them for later removal.
                if (!_airtightQuery.TryComp(ent, out var airtightComp))
                {
                    _deltaPressureInvalidEntityQueue.Enqueue(ent);
                    Log.Error($"DeltaPressure entity without an AirtightComponent found in processing list! Ent: {ent}");
                    return;
                }

                airtightCompsArr[i] = airtightComp;
                var currentPos = airtightComp.LastPosition.Tile;
                var tileBase = i * dirs;

                for (var j = 0; j < dirs; j++)
                {
                    var direction = (AtmosDirection)(1 << j);
                    var offset = currentPos.Offset(direction);
                    tiles[tileBase + j] = gridAtmosComp.Tiles.GetValueOrDefault(offset);
                }
            }

            GetBulkTileAtmospherePressures(tiles, pressures);

            /*
             This entity could be airtight but still be able to contain air on the tile it's on (ex. directional windows).
             As such, substitute the pressure of the pressure on top of the entity for the directions that it can accept air from.
             (Or rather, don't do so for directions that it blocks air from.)
             */
            for (var i = 0; i < len; i++)
            {
                var airtight = airtightCompsArr[i];
                if (airtight.NoAirWhenFullyAirBlocked)
                    continue;

                var currentPos = airtight.LastPosition.Tile;
                var localPressure = 0f;

                // microopting one less nullcheck lmao
                if (gridAtmosComp.Tiles.TryGetValue(currentPos, out var tile) && tile.Air is { } mixture)
                    localPressure = mixture.TotalMoles * Atmospherics.R * mixture.Temperature / mixture.Volume;

                var presBase = i * dirs;
                for (var j = 0; j < dirs; j++)
                {
                    var direction = (AtmosDirection)(1 << j);
                    if (!airtight.AirBlockedDirection.IsFlagSet(direction))
                        pressures[presBase + j] = localPressure;
                }
            }

            /*
             In order to perform SIMD ops we load the values into opposing pairs, where:
             groupA: North, East, South, West
             groupB: South, West, North, East
             That way NumericsHelpers can just do vectorized operations on them super easily.
             */
            for (var i = 0; i < len; i++)
            {
                var presBase = i * dirs;
                var pairBase = i * DeltaPressurePairCount;
                for (var j = 0; j < DeltaPressurePairCount; j++)
                {
                    groupA[pairBase + j] = pressures[presBase + j];
                    groupB[pairBase + j] = pressures[presBase + j + DeltaPressurePairCount];
                }
            }

            // Time to get crankin
            NumericsHelpers.Max(groupA, groupB, groupMax);
            NumericsHelpers.Sub(groupA, groupB);
            NumericsHelpers.Abs(groupA);

            // Now go through each entity and determine their max pressure & delta pressure.
            // Queue for damage if necessary.
            for (var i = 0; i < len; i++)
            {
                var ent = entList[start + i];
                // It is genuinely a massive pain in the ass to handle skipping in the beginning than it is to get that
                // microboost from skipping work. As such, just skip at the very end.
                if (!_random.Prob(ent.Comp.RandomDamageChance))
                {
                    SetIsTakingDamageState(ent, false);
                    continue;
                }

                var pairBase = i * DeltaPressurePairCount;
                var maxPressure = 0f;
                var maxDelta = 0f;
                for (var j = 0; j < DeltaPressurePairCount; j++)
                {
                    // I actually did write a HorizontalMax SIMD method but benchmarking showed that
                    // it was only superior when n > 4. Since we can only compute the max on 4 elements
                    // we can't take advantage of our array being big right here.
                    maxPressure = MathF.Max(maxPressure, groupMax[pairBase + j]);
                    maxDelta = MathF.Max(maxDelta, groupA[pairBase + j]);
                }

                EnqueueDeltaPressureDamage(ent, gridAtmosComp, maxPressure, maxDelta);
            }
        }
        finally
        {
            ArrayPool<TileAtmosphere?>.Shared.Return(tilesArr);
            ArrayPool<AirtightComponent>.Shared.Return(airtightCompsArr);
            ArrayPool<float>.Shared.Return(groupAArr);
            ArrayPool<float>.Shared.Return(groupBArr);
            ArrayPool<float>.Shared.Return(groupMaxArr);
            ArrayPool<float>.Shared.Return(pressuresArr);
        }
    }

    /// <summary>
    /// A DeltaPressure helper method that retrieves the pressures of all gas mixtures
    /// in the given array of <see cref="TileAtmosphere"/>s, and stores the results in the
    /// provided <paramref name="pressures"/> span.
    /// </summary>
    /// <param name="tiles">The tiles span to find the pressures of.</param>
    /// <param name="pressures">The span to store the pressures to - this should be the same length
    /// as the tile array.</param>
    /// <exception cref="ArgumentException">Thrown when the length of the provided spans do not match.</exception>
    private static void GetBulkTileAtmospherePressures(Span<TileAtmosphere?> tiles, Span<float> pressures)
    {
        // this shit is internal because I don't even trust myself
        if (tiles.Length != pressures.Length)
            throw new ArgumentException("Length of Tiles and Pressures span must be the same!");

        var len = pressures.Length;

        // Once again, ArrayPool might return arrays that are longer than the length.
        // We really need them to be all the same length, so slice them here.
        var arr1 = ArrayPool<float>.Shared.Rent(len);
        var arr2 = ArrayPool<float>.Shared.Rent(len);
        var arr3 = ArrayPool<float>.Shared.Rent(len);

        var mixtVol = arr1.AsSpan(0, len);
        var mixtTemp = arr2.AsSpan(0, len);
        var mixtMoles = arr3.AsSpan(0, len);

        try
        {
            for (var i = 0; i < len; i++)
            {
                if (tiles[i] is not { Air: { } mixture })
                {
                    // To prevent any NaN/Div/0 errors, we just bite the bullet
                    // and set everything to the lowest possible value.
                    mixtVol[i] = 1;
                    mixtTemp[i] = 1;
                    mixtMoles[i] = float.Epsilon;
                    continue;
                }

                mixtVol[i] = mixture.Volume;
                mixtTemp[i] = mixture.Temperature;
                mixtMoles[i] = mixture.TotalMoles;
            }

            /*
             Retrieval of single tile pressures requires calling a get method for each tile,
             which does a bunch of scalar operations.

             So we go ahead and batch-retrieve the pressures of all tiles
             and process them in bulk.
             */
            NumericsHelpers.Multiply(mixtMoles, Atmospherics.R);
            NumericsHelpers.Multiply(mixtMoles, mixtTemp);
            NumericsHelpers.Divide(mixtMoles, mixtVol, pressures);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(arr1);
            ArrayPool<float>.Shared.Return(arr2);
            ArrayPool<float>.Shared.Return(arr3);
        }
    }

    /// <summary>
    /// Packs data into a <see cref="DeltaPressureDamageResult"/> data struct and enqueues it
    /// into the <see cref="GridAtmosphereComponent.DeltaPressureDamageResults"/> queue for
    /// later processing.
    /// </summary>
    /// <param name="ent">The entity to enqueue if necessary.</param>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/>
    /// containing the queue.</param>
    /// <param name="pressure">The current absolute pressure being experienced by the entity.</param>
    /// <param name="delta">The current delta pressure being experienced by the entity.</param>
    private void EnqueueDeltaPressureDamage(Entity<DeltaPressureComponent> ent,
        GridAtmosphereComponent gridAtmosComp,
        float pressure,
        float delta)
    {
        var aboveMinPressure = pressure > ent.Comp.MinPressure;
        var aboveMinDeltaPressure = delta > ent.Comp.MinPressureDelta;
        if (!aboveMinPressure && !aboveMinDeltaPressure)
        {
            SetIsTakingDamageState(ent, false);
            return;
        }

        gridAtmosComp.DeltaPressureDamageResults.Enqueue(new DeltaPressureDamageResult(ent,
            pressure,
            delta));
    }

    /// <summary>
    /// Job for solving DeltaPressure entities in parallel.
    /// Batches are given some index to start from, so each thread can simply just start at that index
    /// and process the next n entities in the list.
    /// </summary>
    /// <param name="system">The AtmosphereSystem instance.</param>
    /// <param name="atmosphere">The GridAtmosphereComponent to work with.</param>
    /// <param name="startIndex">The index in the DeltaPressureEntities list to start from.</param>
    /// <param name="cvarBatchSize">The batch size to use for this job.</param>
    private sealed class DeltaPressureParallelBulkJob(
        AtmosphereSystem system,
        GridAtmosphereComponent atmosphere,
        int startIndex,
        int cvarBatchSize)
        : IParallelBulkRobustJob
    {
        public int BatchSize => cvarBatchSize;

        public void ExecuteRange(int start, int end)
        {
            system.ProcessDeltaPressureEntityBulk(atmosphere, start + startIndex, end + startIndex);
        }
    }

    /// <summary>
    /// Struct that holds the result of delta pressure damage processing for an entity.
    /// This is only created and enqueued when the entity needs to take damage.
    /// </summary>
    /// <param name="Ent">The entity to deal damage to.</param>
    /// <param name="Pressure">The current absolute pressure the entity is experiencing.</param>
    /// <param name="DeltaPressure">The current delta pressure the entity is experiencing.</param>
    public readonly record struct DeltaPressureDamageResult(
        Entity<DeltaPressureComponent> Ent,
        float Pressure,
        float DeltaPressure);

    /// <summary>
    /// Does damage to an entity depending on the pressure experienced by it, based on the
    /// entity's <see cref="DeltaPressureComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to apply damage to.</param>
    /// <param name="pressure">The absolute pressure being exerted on the entity.</param>
    /// <param name="deltaPressure">The delta pressure being exerted on the entity.</param>
    private void PerformDamage(Entity<DeltaPressureComponent> ent, float pressure, float deltaPressure)
    {
        var maxPressure = Math.Max(pressure - ent.Comp.MinPressure, deltaPressure - ent.Comp.MinPressureDelta);
        var maxPressureCapped = Math.Min(maxPressure, ent.Comp.MaxEffectivePressure);
        var appliedDamage = ScaleDamage(ent, ent.Comp.BaseDamage, maxPressureCapped);

        _damage.ChangeDamage(ent.Owner, appliedDamage, ignoreResistances: true, interruptsDoAfters: false);
        SetIsTakingDamageState(ent, true);
    }

    /// <summary>
    /// Helper function to prevent spamming clients with dirty events when the damage state hasn't changed.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <param name="toSet">The value to set.</param>
    private void SetIsTakingDamageState(Entity<DeltaPressureComponent> ent, bool toSet)
    {
        if (ent.Comp.IsTakingDamage == toSet)
            return;
        ent.Comp.IsTakingDamage = toSet;
        Dirty(ent);
    }

    /// <summary>
    /// Returns a new DamageSpecifier scaled based on values on an entity with a DeltaPressureComponent.
    /// </summary>
    /// <param name="ent">The entity to base the manipulations off of (pull scaling type)</param>
    /// <param name="damage">The base damage specifier to scale.</param>
    /// <param name="pressure">The pressure being exerted on the entity.</param>
    /// <returns>A scaled DamageSpecifier.</returns>
    private static DamageSpecifier ScaleDamage(Entity<DeltaPressureComponent> ent, DamageSpecifier damage, float pressure)
    {
        var factor = ent.Comp.ScalingType switch
        {
            DeltaPressureDamageScalingType.Threshold => 1f,
            DeltaPressureDamageScalingType.Linear => pressure * ent.Comp.ScalingPower,
            DeltaPressureDamageScalingType.Log =>
                (float) Math.Log(pressure, ent.Comp.ScalingPower),
            _ => throw new ArgumentOutOfRangeException(nameof(ent), "Invalid damage scaling type!"),
        };

        return damage * factor;
    }
}

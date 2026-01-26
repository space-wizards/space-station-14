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
    /// Processes a singular entity, determining the pressures it's experiencing and applying damage based on that.
    /// </summary>
    /// <param name="ent">The entity to process.</param>
    /// <param name="gridAtmosComp">The <see cref="GridAtmosphereComponent"/> that belongs to the entity's GridUid.</param>
    private void ProcessDeltaPressureEntity(Entity<DeltaPressureComponent> ent, GridAtmosphereComponent gridAtmosComp)
    {
        if (!_random.Prob(ent.Comp.RandomDamageChance))
            return;

        /*
         To make our comparisons a little bit faster, we take advantage of SIMD-accelerated methods
         in the NumericsHelpers class.

         This involves loading our values into a span in the form of opposing pairs,
         so simple vector operations like min/max/abs can be performed on them.
         */

        var airtightComp = _airtightQuery.Comp(ent);
        var currentPos = airtightComp.LastPosition.Tile;
        var tiles = new TileAtmosphere?[Atmospherics.Directions];
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            var offset = currentPos.Offset(direction);
            tiles[i] = gridAtmosComp.Tiles.GetValueOrDefault(offset);
        }

        Span<float> pressures = stackalloc float[Atmospherics.Directions];

        GetBulkTileAtmospherePressures(tiles, pressures);

        // This entity could be airtight but still be able to contain air on the tile it's on (ex. directional windows).
        // As such, substitute the pressure of the pressure on top of the entity for the directions that it can accept air from.
        // (Or rather, don't do so for directions that it blocks air from.)
        if (!airtightComp.NoAirWhenFullyAirBlocked)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                if (!airtightComp.AirBlockedDirection.HasFlag(direction))
                {
                    pressures[i] = gridAtmosComp.Tiles.GetValueOrDefault(currentPos)?.Air?.Pressure ?? 0f;
                }
            }
        }

        Span<float> opposingGroupA = stackalloc float[DeltaPressurePairCount];
        Span<float> opposingGroupB = stackalloc float[DeltaPressurePairCount];
        Span<float> opposingGroupMax = stackalloc float[DeltaPressurePairCount];

        // Directions are always in pairs: the number of directions is always even
        // (we must consider the future where Multi-Z is real)
        // Load values into opposing pairs.
        for (var i = 0; i < DeltaPressurePairCount; i++)
        {
            opposingGroupA[i] = pressures[i];
            opposingGroupB[i] = pressures[i + DeltaPressurePairCount];
        }

        // TODO ATMOS: Needs to be changed to batch operations so that more operations can actually be done in parallel.

        // Need to determine max pressure in opposing directions for absolute pressure calcs.
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
    /// A DeltaPressure helper method that retrieves the pressures of all gas mixtures
    /// in the given array of <see cref="TileAtmosphere"/>s, and stores the results in the
    /// provided <paramref name="pressures"/> span.
    /// The tiles array length is limited to Atmosphereics.Directions.
    /// </summary>
    /// <param name="tiles">The tiles array to find the pressures of.</param>
    /// <param name="pressures">The span to store the pressures to - this should be the same length
    /// as the tile array.</param>
    /// <remarks>This is for internal use of the DeltaPressure system -
    /// it may not be a good idea to use this generically.</remarks>
    private static void GetBulkTileAtmospherePressures(TileAtmosphere?[] tiles, Span<float> pressures)
    {
        #if DEBUG
        // Just in case someone tries to use this method incorrectly.
        if (tiles.Length != pressures.Length || tiles.Length != Atmospherics.Directions)
            throw new ArgumentException("Length of arrays must be the same and of Atmospherics.Directions length.");
        #endif

        // This hardcoded direction limit is stopping goobers from
        // overflowing the stack with massive arrays.
        // If this method is pulled into a more generic place,
        // it should be replaced with method params.
        Span<float> mixtVol = stackalloc float[Atmospherics.Directions];
        Span<float> mixtTemp = stackalloc float[Atmospherics.Directions];
        Span<float> mixtMoles = stackalloc float[Atmospherics.Directions];
        Span<float> atmosR = stackalloc float[Atmospherics.Directions];

        for (var i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] is not { Air: { } mixture })
            {
                pressures[i] = 0f;

                // To prevent any NaN/Div/0 errors, we just bite the bullet
                // and set everything to the lowest possible value.
                mixtVol[i] = 1;
                mixtTemp[i] = 1;
                mixtMoles[i] = float.Epsilon;
                atmosR[i] = 1;
                continue;
            }

            mixtVol[i] = mixture.Volume;
            mixtTemp[i] = mixture.Temperature;
            mixtMoles[i] = mixture.TotalMoles;
            atmosR[i] = Atmospherics.R;
        }

        /*
         Retrieval of single tile pressures requires calling a get method for each tile,
         which does a bunch of scalar operations.

         So we go ahead and batch-retrieve the pressures of all tiles
         and process them in bulk.
         */
        NumericsHelpers.Multiply(mixtMoles, atmosR);
        NumericsHelpers.Multiply(mixtMoles, mixtTemp);
        NumericsHelpers.Divide(mixtMoles, mixtVol, pressures);
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
    private sealed class DeltaPressureParallelJob(
        AtmosphereSystem system,
        GridAtmosphereComponent atmosphere,
        int startIndex,
        int cvarBatchSize)
        : IParallelRobustJob
    {
        public int BatchSize => cvarBatchSize;

        public void Execute(int index)
        {
            // The index is relative to the startIndex (because we can pause and resume computation),
            // so we need to add it to the startIndex.
            var actualIndex = startIndex + index;

            if (actualIndex >= atmosphere.DeltaPressureEntities.Count)
                return;

            var ent = atmosphere.DeltaPressureEntities[actualIndex];
            system.ProcessDeltaPressureEntity(ent, atmosphere);
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

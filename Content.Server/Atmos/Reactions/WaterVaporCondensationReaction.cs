using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Atmos.Reactions;

[DataDefinition]
public sealed partial class WaterVaporCondensationReaction : IGasReactionEffect
{
    private const string Reagent = "Water";
    private const float CondensationRate = 0.05f;
    private const int MaxTilesCap = 100; // Max tiles processed per tick
    private const float MaxCondensationPressure = 200f;
    private const float MolesToUnitsMultiplier = 2f;

    private static readonly HashSet<TileAtmosphere> EligibleTiles = new();
    private static readonly Random Random = new();

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        // Only react on tile atmospheres
        if (holder is not TileAtmosphere tile)
            return ReactionResult.NoReaction;

        // Skip if pressure is too high
        if (mixture.Pressure > MaxCondensationPressure)
            return ReactionResult.NoReaction;

        EligibleTiles.Add(tile);
        return ReactionResult.Reacting;
    }

    public static void ProcessEligibleTiles(AtmosphereSystem system)
    {
        // Early exit if no tiles to process
        if (EligibleTiles.Count == 0)
            return;

        // Dynamic processing limit (10% of eligible tiles, capped at MaxTilesCap)
        var dynamicLimit = Math.Min((int)Math.Ceiling(EligibleTiles.Count * 0.1), MaxTilesCap);
        var processedAmount = 0;

        // Shuffle all the tiles so that the system does not select the same tiles every tick
        var tilesList = EligibleTiles.ToList();
        for (var i = tilesList.Count - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (tilesList[i], tilesList[j]) = (tilesList[j], tilesList[i]);
        }
        EligibleTiles.Clear();

        // Process each tile
        foreach (var tile in tilesList)
        {
            // Limit
            if (processedAmount >= dynamicLimit)
                break;

            // Protection against NullReferenceException
            if (tile.Air?.GetMoles(Gas.WaterVapor) is not > 0)
                continue;

            // Get tile reference
            var tileRef = system.GetTileRef(tile);

            // Calculate
            var currentMoles = tile.Air.GetMoles(Gas.WaterVapor);
            var amountToCondense = Math.Min(currentMoles, CondensationRate);

            // Create water puddle
            FixedPoint2 reagentAmount = FixedPoint2.New(amountToCondense * MolesToUnitsMultiplier);
            system.Puddle.TrySpillAt(tileRef, new Solution(Reagent, reagentAmount), out _, sound: false);

            // Adjust gas mixture
            tile.Air.AdjustMoles(Gas.WaterVapor, -amountToCondense);

            processedAmount++;
        }
    }
}

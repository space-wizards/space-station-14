using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Compares two TileAtmospheres to see if they are within acceptable ranges for group processing to be enabled.
    /// </summary>
    [PublicAPI]
    public GasCompareResult CompareExchange(TileAtmosphere sample, TileAtmosphere otherSample)
    {
        if (sample.AirArchived == null || otherSample.AirArchived == null)
            return GasCompareResult.NoExchange;

        return CompareExchange(sample.AirArchived, otherSample.AirArchived);
    }

    /// <summary>
    /// Compares two gas mixtures to see if they are within acceptable ranges for group processing to be enabled.
    /// </summary>
    [PublicAPI]
    public GasCompareResult CompareExchange(GasMixture sample, GasMixture otherSample)
    {
        var moles = 0f;

        for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasMoles = sample.Moles[i];
            var delta = MathF.Abs(gasMoles - otherSample.Moles[i]);
            if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                return (GasCompareResult)i; // We can move gases!
            moles += gasMoles;
        }

        if (moles > Atmospherics.MinimumMolesDeltaToMove)
        {
            var tempDelta = MathF.Abs(sample.Temperature - otherSample.Temperature);
            if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                return GasCompareResult.TemperatureExchange; // There can be temperature exchange.
        }

        // No exchange at all!
        return GasCompareResult.NoExchange;
    }

    /// <summary>
    /// Performs reactions for a given gas mixture on an optional holder.
    /// </summary>
    [PublicAPI]
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
    {
        var reaction = ReactionResult.NoReaction;
        var temperature = mixture.Temperature;
        var energy = GetThermalEnergy(mixture);

        foreach (var prototype in GasReactions)
        {
            if (energy < prototype.MinimumEnergyRequirement ||
                temperature < prototype.MinimumTemperatureRequirement ||
                temperature > prototype.MaximumTemperatureRequirement)
                continue;

            var doReaction = true;
            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var req = prototype.MinimumRequirements[i];

                if (!(mixture.GetMoles(i) < req))
                    continue;

                doReaction = false;
                break;
            }

            if (!doReaction)
                continue;

            reaction = prototype.React(mixture, holder, this, HeatScale);
            if(reaction.HasFlag(ReactionResult.StopReactions))
                break;
        }

        return reaction;
    }

    /// <summary>
    /// Triggers a tile's <see cref="GasMixture"/> to react.
    /// </summary>
    /// <param name="gridId">The grid to react the tile on.</param>
    /// <param name="tile">The tile to react.</param>
    /// <returns>The result of the reaction.</returns>
    [PublicAPI]
    public ReactionResult ReactTile(EntityUid gridId, Vector2i tile)
    {
        var ev = new ReactTileMethodEvent(gridId, tile);
        RaiseLocalEvent(gridId, ref ev);

        ev.Handled = true;

        return ev.Result;
    }
}

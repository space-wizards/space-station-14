using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(Entity<TransformComponent?> ent,
        bool ignoreExposed = false,
        bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return GetContainingMixture(ent, ent.Comp.GridUid, ent.Comp.MapUid, ignoreExposed, excite);
    }

    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="grid">The grid that the entity may be on.</param>
    /// <param name="map">The map that the entity may be on.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(
        Entity<TransformComponent?> ent,
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        bool ignoreExposed = false,
        bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (!ignoreExposed && !ent.Comp.Anchored)
        {
            // Used for things like disposals/cryo to change which air people are exposed to.
            var ev = new AtmosExposedGetAirEvent((ent, ent.Comp), excite);
            RaiseLocalEvent(ent, ref ev);
            if (ev.Handled)
                return ev.Gas;

            // TODO ATMOS: recursively iterate up through parents
            // This really needs recursive InContainer metadata flag for performance
            // And ideally some fast way to get the innermost airtight container.
        }

        var position = _transformSystem.GetGridTilePositionOrDefault((ent, ent.Comp));
        return GetTileMixture(grid, map, position, excite);
    }

    /// <summary>
    /// Gets all <see cref="TileAtmosphere"/> <see cref="GasMixture"/>s on a grid.
    /// </summary>
    /// <param name="gridUid">The grid to get mixtures for.</param>
    /// <param name="excite">Whether to mark all tiles as active for atmosphere processing.</param>
    /// <returns>An enumerable of all gas mixtures on the grid.</returns>
    [PublicAPI]
    public IEnumerable<GasMixture> GetAllMixtures(EntityUid gridUid, bool excite = false)
    {
        var ev = new GetAllMixturesMethodEvent(gridUid, excite);
        RaiseLocalEvent(gridUid, ref ev);

        if (!ev.Handled)
            return [];

        DebugTools.AssertNotNull(ev.Mixtures);
        return ev.Mixtures!;
    }

    /// <summary>
    /// Gets the gas mixtures for a list of tiles on a grid or map.
    /// </summary>
    /// <param name="grid">The grid to get mixtures from.</param>
    /// <param name="map">The map to get mixtures from.</param>
    /// <param name="tiles">The list of tiles to get mixtures for.</param>
    /// <param name="excite">Whether to mark the tiles as active for atmosphere processing.</param>
    /// <returns>>An array of gas mixtures corresponding to the input tiles.</returns>
    [PublicAPI]
    public GasMixture?[]? GetTileMixtures(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        List<Vector2i> tiles,
        bool excite = false)
    {
        GasMixture?[]? mixtures = null;
        var handled = false;

        // If we've been passed a grid, try to let it handle it.
        if (grid is { } gridEnt && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp1))
        {
            if (excite)
                Resolve(gridEnt, ref gridEnt.Comp2);

            handled = true;
            mixtures = new GasMixture?[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (!gridEnt.Comp1.Tiles.TryGetValue(tile, out var atmosTile))
                {
                    // need to get map atmosphere
                    handled = false;
                    continue;
                }

                mixtures[i] = atmosTile.Air;

                if (excite)
                {
                    AddActiveTile(gridEnt.Comp1, atmosTile);
                    InvalidateVisuals((gridEnt.Owner, gridEnt.Comp2), tile);
                }
            }
        }

        if (handled)
            return mixtures;

        // We either don't have a grid, or the event wasn't handled.
        // Let the map handle it instead, and also broadcast the event.
        if (map is { } mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp))
        {
            mixtures ??= new GasMixture?[tiles.Count];
            for (var i = 0; i < tiles.Count; i++)
            {
                mixtures[i] ??= mapEnt.Comp.Mixture;
            }

            return mixtures;
        }

        // Default to a space mixture... This is a space game, after all!
        mixtures ??= new GasMixture?[tiles.Count];
        for (var i = 0; i < tiles.Count; i++)
        {
            mixtures[i] ??= GasMixture.SpaceGas;
        }

        return mixtures;
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile that an entity is on.
    /// </summary>
    /// <param name="entity">The entity to get the tile mixture for.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    /// <remarks>This does not return the <see cref="GasMixture"/> that the entity
    /// may be contained in, ex. if the entity is currently in a locker/crate with its own
    /// <see cref="GasMixture"/>.</remarks>
    [PublicAPI]
    public GasMixture? GetTileMixture(Entity<TransformComponent?> entity, bool excite = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return null;

        var indices = _transformSystem.GetGridTilePositionOrDefault(entity);
        return GetTileMixture(entity.Comp.GridUid, entity.Comp.MapUid, indices, excite);
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile on a grid or map.
    /// </summary>
    /// <param name="grid">The grid to get the mixture from.</param>
    /// <param name="map">The map to get the mixture from.</param>
    /// <param name="gridTile">The tile to get the mixture from.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetTileMixture(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        Vector2i gridTile,
        bool excite = false)
    {
        // If we've been passed a grid, try to let it handle it.
        if (grid is { } gridEnt
            && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp1, false)
            && gridEnt.Comp1.Tiles.TryGetValue(gridTile, out var tile))
        {
            if (excite)
            {
                AddActiveTile(gridEnt.Comp1, tile);
                InvalidateVisuals((grid.Value.Owner, grid.Value.Comp2), gridTile);
            }

            return tile.Air;
        }

        if (map is { } mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp, false))
            return mapEnt.Comp.Mixture;

        // Default to a space mixture... This is a space game, after all!
        return GasMixture.SpaceGas;
    }

    [PublicAPI]
    public override bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
        NumericsHelpers.Multiply(mixture.Moles, GasFuelMask, tmp);
        return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
    }

    [PublicAPI]
    public override bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        Span<float> tmp = stackalloc float[Atmospherics.AdjustedNumberOfGases];
        NumericsHelpers.Multiply(mixture.Moles, GasOxidizerMask, tmp);
        return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
    }

    /// <summary>
    /// Merges the <see cref="giver"/> gas mixture into the <see cref="receiver"/> gas mixture.
    /// The <see cref="giver"/> gas mixture is not modified by this method.
    /// </summary>
    [PublicAPI]
    public void Merge(GasMixture receiver, GasMixture giver)
    {
        if (receiver.Immutable)
            return;

        if (MathF.Abs(receiver.Temperature - giver.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
        {
            var receiverHeatCapacity = GetHeatCapacity(receiver);
            var giverHeatCapacity = GetHeatCapacity(giver);
            var combinedHeatCapacity = receiverHeatCapacity + giverHeatCapacity;
            if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
            {
                receiver.Temperature = (GetThermalEnergy(giver, giverHeatCapacity) + GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
            }
        }

        NumericsHelpers.Add(receiver.Moles, giver.Moles);
    }

    /// <summary>
    /// Divides a source gas mixture into several recipient mixtures, scaled by their relative volumes. Does not
    /// modify the source gas mixture. Used for pipe network splitting. Note that the total destination volume
    /// may be larger or smaller than the source mixture.
    /// </summary>
    [PublicAPI]
    public void DivideInto(GasMixture source, List<GasMixture> receivers)
    {
        var totalVolume = 0f;
        foreach (var receiver in receivers)
        {
            if (!receiver.Immutable)
                totalVolume += receiver.Volume;
        }

        float? sourceHeatCapacity = null;
        var buffer = new float[Atmospherics.AdjustedNumberOfGases];

        foreach (var receiver in receivers)
        {
            if (receiver.Immutable)
                continue;

            var fraction = receiver.Volume / totalVolume;

            // Set temperature, if necessary.
            if (MathF.Abs(receiver.Temperature - source.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                // Often this divides a pipe net into new and completely empty pipe nets
                if (receiver.TotalMoles == 0)
                    receiver.Temperature = source.Temperature;
                else
                {
                    sourceHeatCapacity ??= GetHeatCapacity(source);
                    var receiverHeatCapacity = GetHeatCapacity(receiver);
                    var combinedHeatCapacity = receiverHeatCapacity + sourceHeatCapacity.Value * fraction;
                    if (combinedHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    {
                        receiver.Temperature =
                            (GetThermalEnergy(source, sourceHeatCapacity.Value * fraction) +
                             GetThermalEnergy(receiver, receiverHeatCapacity)) / combinedHeatCapacity;
                    }
                }
            }

            // transfer moles
            NumericsHelpers.Multiply(source.Moles, fraction, buffer);
            NumericsHelpers.Add(receiver.Moles, buffer);
        }
    }

    /// <summary>
    /// Releases gas from this mixture to the output mixture.
    /// If the output mixture is null, then this is being released into space.
    /// It can't transfer air to a mixture with higher pressure.
    /// </summary>
    [PublicAPI]
    public bool ReleaseGasTo(GasMixture mixture, GasMixture? output, float targetPressure)
    {
        var outputStartingPressure = output?.Pressure ?? 0;
        var inputStartingPressure = mixture.Pressure;

        if (outputStartingPressure >= MathF.Min(targetPressure, inputStartingPressure - 10))
            // No need to pump gas if the target is already reached or input pressure is too low.
            // Need at least 10 kPa difference to overcome friction in the mechanism.
            return false;

        if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0))
            return false;

        // We calculate the necessary moles to transfer with the ideal gas law.
        var pressureDelta = MathF.Min(targetPressure - outputStartingPressure, (inputStartingPressure - outputStartingPressure) / 2f);
        var transferMoles = pressureDelta * (output?.Volume ?? Atmospherics.CellVolume) / (mixture.Temperature * Atmospherics.R);

        // And now we transfer the gas.
        var removed = mixture.Remove(transferMoles);

        if(output != null)
            Merge(output, removed);

        return true;
    }

    /// <summary>
    /// Pump gas from this mixture to the output mixture.
    /// Amount depends on target pressure.
    /// </summary>
    /// <param name="mixture">The mixture to pump the gas from</param>
    /// <param name="output">The mixture to pump the gas to</param>
    /// <param name="targetPressure">The target pressure to reach</param>
    /// <returns>Whether we could pump air to the output or not</returns>
    [PublicAPI]
    public bool PumpGasTo(GasMixture mixture, GasMixture output, float targetPressure)
    {
        var outputStartingPressure = output.Pressure;
        var pressureDelta = targetPressure - outputStartingPressure;

        if (pressureDelta < 0.01)
            // No need to pump gas, we've reached the target.
            return false;

        if (!(mixture.TotalMoles > 0) || !(mixture.Temperature > 0))
            return false;

        // We calculate the necessary moles to transfer with the ideal gas law.
        var transferMoles = pressureDelta * output.Volume / (mixture.Temperature * Atmospherics.R);

        // And now we transfer the gas.
        var removed = mixture.Remove(transferMoles);
        Merge(output, removed);
        return true;
    }

    /// <summary>
    /// Scrubs specified gases from a gas mixture into a <see cref="destination"/> gas mixture.
    /// </summary>
    [PublicAPI]
    public void ScrubInto(GasMixture mixture, GasMixture destination, IReadOnlyCollection<Gas> filterGases)
    {
        var buffer = new GasMixture(mixture.Volume){Temperature = mixture.Temperature};

        foreach (var gas in filterGases)
        {
            buffer.AdjustMoles(gas, mixture.GetMoles(gas));
            mixture.SetMoles(gas, 0f);
        }

        Merge(destination, buffer);
    }

    /// <summary>
    /// Calculates the dimensionless fraction of gas required to equalize pressure between two gas mixtures.
    /// </summary>
    /// <param name="gasMixture1">The first gas mixture involved in the pressure equalization.
    /// This mixture should be the one you always expect to be the highest pressure.</param>
    /// <param name="gasMixture2">The second gas mixture involved in the pressure equalization.</param>
    /// <returns>A float (from 0 to 1) representing the dimensionless fraction of gas that needs to be transferred from the
    /// mixture of higher pressure to the mixture of lower pressure.</returns>
    /// <remarks>
    /// <para>
    /// This properly takes into account the effect
    /// of gas merging from inlet to outlet affecting the temperature
    /// (and possibly increasing the pressure) in the outlet.
    /// </para>
    /// <para>
    /// The gas is assumed to expand freely,
    /// so the temperature of the gas with the greater pressure is not changing.
    /// </para>
    /// </remarks>
    /// <example>
    /// If you want to calculate the moles required to equalize pressure between an inlet and an outlet,
    /// multiply the fraction returned by the source moles.
    /// </example>
    [PublicAPI]
    public float FractionToEqualizePressure(GasMixture gasMixture1, GasMixture gasMixture2)
    {
        /*
        Problem: the gas being merged from the inlet to the outlet could affect the
        temp. of the gas and cause a pressure rise.
        We want the pressure to be equalized, so we have to account for this.

        For clarity, let's assume that gasMixture1 is the inlet and gasMixture2 is the outlet.

        We require mechanical equilibrium, so \( P_1' = P_2' \)

        Before the transfer, we have:
        \( P_1 = \frac{n_1 R T_1}{V_1} \)
        \( P_2 = \frac{n_2 R T_2}{V_2} \)

        After removing fraction \( x \) moles from the inlet, we have:
        \( P_1' = \frac{(1 - x) n_1 R T_1}{V_1} \)

        The outlet will gain the same \( x n_1 \) moles of gas.
        So \( n_2' = n_2 + x n_1 \)

        After mixing, the outlet temperature will be changed.
        Denote the new mixture temperature as \( T_2' \).
        Volume is constant.
        So we have:
        \( P_2' = \frac{(n_2 + x n_1) R T_2}{V_2} \)

        The total energy of the incoming inlet to outlet gas at \( T_1 \) plus the existing energy of the outlet gas at \( T_2 \)
        will be equal to the energy of the new outlet gas at \( T_2' \).
        This leads to the following derivation:
        \( x n_1 C_1 T_1 + n_2 C_2 T_2 = (x n_1 C_1 + n_2 C_2) T_2' \)

        Where \( C_1 \) and \( C_2 \) are the heat capacities of the inlet and outlet gases, respectively.

        Solving for \( T_2' \) gives us:
        \( T_2' = \frac{x n_1 C_1 T_1 + n_2 C_2 T_2}{x n_1 C_1 + n_2 C_2} \)

        Once again, we require mechanical equilibrium (\( P_1' = P_2' \)),
        so we can substitute \( T_2' \) into the pressure equation:

        \( \frac{(1 - x) n_1 R T_1}{V_1} =
        \frac{(n_2 + x n_1) R}{V_2} \cdot
        \frac{x n_1 C_1 T_1 + n_2 C_2 T_2}
        {x n_1 C_1 + n_2 C_2} \)

        Now it's a matter of solving for \( x \).
        Not going to show the full derivation here, just steps.
        1. Cancel common factor \( R \).
        2. Multiply both sides by \( x n_1 C_1 + n_2 C_2 \), so that everything
        becomes a polynomial in terms of \( x \).
        3. Expand both sides.
        4. Collect like powers of \( x \).
        5. After collecting, you should end up with a polynomial of the form:

        \( (-n_1 C_1 T_1 (1 + \frac{V_2}{V_1})) x^2 +
        (n_1 T_1 \frac{V_2}{V_1} (C_1 - C_2) - n_2 C_1 T_1 - n_1 C_2 T_2) x +
        (n_1 T_1 \frac{V_2}{V_1} C_2 - n_2 C_2 T_2) = 0 \)

        Divide through by \( n_1 C_1 T_1 \) and replace each ratio with a symbol for clarity:
        \( k_V = \frac{V_2}{V_1} \)
        \( k_n = \frac{n_2}{n_1} \)
        \( k_T = \frac{T_2}{T_1} \)
        \( k_C = \frac{C_2}{C_1} \)
        */

        // Ensure that P_1 > P_2 so the quadratic works out.
        if (gasMixture1.Pressure < gasMixture2.Pressure)
        {
            (gasMixture1, gasMixture2) = (gasMixture2, gasMixture1);
        }

        // Establish the dimensionless ratios.
        var volumeRatio = gasMixture2.Volume / gasMixture1.Volume;
        var molesRatio = gasMixture2.TotalMoles / gasMixture1.TotalMoles;
        var temperatureRatio = gasMixture2.Temperature / gasMixture1.Temperature;
        var heatCapacityRatio = GetHeatCapacity(gasMixture2) / GetHeatCapacity(gasMixture1);

        // The quadratic equation is solved for the transfer fraction.
        var quadraticA = 1 + volumeRatio;
        var quadraticB = molesRatio - volumeRatio + heatCapacityRatio * (temperatureRatio + volumeRatio);
        var quadraticC = heatCapacityRatio * (molesRatio * temperatureRatio - volumeRatio);

        return (-quadraticB + MathF.Sqrt(quadraticB * quadraticB - 4 * quadraticA * quadraticC)) / (2 * quadraticA);
    }

    /// <summary>
    /// Determines the fraction of gas to be removed and transferred from a source
    /// <see cref="GasMixture"/> to a target <see cref="GasMixture"/> to reach a target pressure
    /// in the target <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mix1">The source <see cref="GasMixture"/> that gas will be removed from.
    /// This should always be of higher pressure than the second <see cref="GasMixture"/>.</param>
    /// <param name="mix2">The target <see cref="GasMixture"/> that will increase in pressure
    /// to the target pressure.</param>
    /// <param name="targetPressure">The target mixture's desired pressure to target.</param>
    /// <returns>A float representing the dimensionless fraction of gas to transfer from the source
    /// to the target. This may return negative if you have your mixtures swapped.</returns>
    /// <remarks>Note that this method doesn't take into account the heat capacity of the
    /// transferred volume causing a pressure rise in the target <see cref="GasMixture"/>.</remarks>
    [PublicAPI]
    public static float FractionToMaxPressure(GasMixture mix1, GasMixture mix2, float targetPressure)
    {
        var molesToTransfer = MolesToMaxPressure(mix1, mix2, targetPressure);
        return molesToTransfer / mix1.TotalMoles;
    }

    /// <summary>
    /// Determines the number of moles to be removed and transferred from a source
    /// <see cref="GasMixture"/> to a target <see cref="GasMixture"/> to reach a target pressure
    /// in the target <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mix1">The source <see cref="GasMixture"/> that gas will be removed from.
    /// This should always be of higher pressure than the second <see cref="GasMixture"/>.</param>
    /// <param name="mix2">The target <see cref="GasMixture"/> that will increase in pressure
    /// to the target pressure.</param>
    /// <param name="targetPressure">The target mixture's desired pressure to target.</param>
    /// <returns>The difference in moles required to reach the target pressure.</returns>
    /// <remarks>Note that this method doesn't take into account the heat capacity of the
    /// transferred volume causing a pressure rise in the target <see cref="GasMixture"/>.</remarks>
    [PublicAPI]
    public static float MolesToMaxPressure(GasMixture mix1, GasMixture mix2, float targetPressure)
    {
        /*
         Calculate the moles required to reach the target pressure.
         The formula is derived from the ideal gas law and the
         general Richman's law, under the simplification that all the specific heat capacities are equal.
         Derivation can also be seen at
         https://github.com/space-wizards/space-station-14/pull/35211/files/a0ae787fe07a4e792570f55b49d9dd8038eb6e4d#r1961183456
         TODO ATMOS Make this properly obey the heat capacity change on the target mixture.

         Derivation is as follows.
         Assume A is mix1, B is mix2, C is the combined mixture after transfer.
         We can express the number of moles in C:
         n_C = n_A + n_B

         We can then determine the temperature of C:
         T_C = \frac{T_A n_A c_A + T_B n_B c_B}{n_A c_A + n_B c_B}

         Where c_A and c_B are the specific heats of mixtures A and B, respectively.
         We can then express the pressure of C:
         P_C = \frac{n_C R T_C}{V_C}

         Using the above equations, we can express P_C as follows:
         P_C = \frac{(n_A + n_B) R (\frac{T_a n_A + T_B n_B}{n_A + n_B}}{V_C}

         Which can be reduced to:
         P_C = \frac{R (T_A n_A + T_B n_B)}{V_C}

         Solving for n_A gives:
         n_A = \frac{P_C V_C - R T_B n_B}{R T_A}

         Using the ideal gas law to substitute:
         n_A = \frac{P_C V_C - P_B V_B}{R T_A}

         The output volume doesn't change:
         V_B = V_C

         So:
         n_A = \frac{(P_C - P_B) V_B}{R T_A}
         */

        var delta = targetPressure - mix2.Pressure;
        var requiredMoles = (delta * mix2.Volume) / (mix1.Temperature * Atmospherics.R);

        // Return the fraction of moles to transfer.
        return requiredMoles;
    }

    /// <summary>
    /// Determines the number of moles that need to be removed from a <see cref="GasMixture"/> to reach a target pressure threshold.
    /// </summary>
    /// <param name="gasMixture">The gas mixture whose moles and properties will be used in the calculation.</param>
    /// <param name="targetPressure">The target pressure threshold to calculate against.</param>
    /// <returns>The difference in moles required to reach the target pressure threshold.</returns>
    /// <remarks>The temperature of the gas is assumed to be not changing due to a free expansion.</remarks>
    [PublicAPI]
    public static float MolesToPressureThreshold(GasMixture gasMixture, float targetPressure)
    {
        // Kid named PV = nRT.
        return gasMixture.TotalMoles -
               targetPressure * gasMixture.Volume / (Atmospherics.R * gasMixture.Temperature);
    }

    /// <summary>
    /// Adds an array of moles to a <see cref="GasMixture"/>.
    /// Guards against negative moles by clamping to zero.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to add moles to.</param>
    /// <param name="molsToAdd">The <see cref="ReadOnlySpan{T}"/> of moles to add.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length of the <see cref="ReadOnlySpan{T}"/>
    /// is not the same as the length of the <see cref="GasMixture"/> gas array.</exception>
    [PublicAPI]
    public static void AddMolsToMixture(GasMixture mixture, ReadOnlySpan<float> molsToAdd)
    {
        // Span length should be as long as the length of the gas array.
        // Technically this is a redundant check because NumericsHelpers will do the same thing,
        // but eh.
        ArgumentOutOfRangeException.ThrowIfNotEqual(mixture.Moles.Length, molsToAdd.Length, nameof(mixture.Moles.Length));

        NumericsHelpers.Add(mixture.Moles, molsToAdd);
        NumericsHelpers.Max(mixture.Moles, 0f);
    }

    /// <summary>
    /// Gets the particular price of a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to get the price of.</param>
    /// <returns>The price of the gas mixture.</returns>
    [PublicAPI]
    public double GetPrice(GasMixture mixture)
    {
        float basePrice = 0; // moles of gas * price/mole
        float totalMoles = 0; // total number of moles in can
        float maxComponent = 0; // moles of the dominant gas
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            basePrice += mixture.Moles[i] * GetGas(i).PricePerMole;
            totalMoles += mixture.Moles[i];
            maxComponent = Math.Max(maxComponent, mixture.Moles[i]);
        }

        // Pay more for gas canisters that are purer
        float purity = 1;
        if (totalMoles > 0)
        {
            purity = maxComponent / totalMoles;
        }

        return basePrice * purity;
    }

    /// <summary>
    /// Calculates the thermal energy for a gas mixture.
    /// </summary>
    [PublicAPI]
    public float GetThermalEnergy(GasMixture mixture)
    {
        return mixture.Temperature * GetHeatCapacity(mixture);
    }

    /// <summary>
    /// Calculates the thermal energy for a gas mixture, using a cached heat capacity value.
    /// </summary>
    [PublicAPI]
    public float GetThermalEnergy(GasMixture mixture, float cachedHeatCapacity)
    {
        return mixture.Temperature * cachedHeatCapacity;
    }

    /// <summary>
    /// Add 'dQ' Joules of energy into 'mixture'.
    /// </summary>
    [PublicAPI]
    public void AddHeat(GasMixture mixture, float dQ)
    {
        var c = GetHeatCapacity(mixture);
        var dT = dQ / c;
        mixture.Temperature += dT;
    }

    /// <summary>
    /// Checks whether a gas mixture is probably safe.
    /// This only checks temperature and pressure, not gas composition.
    /// </summary>
    /// <param name="air">Mixture to be checked.</param>
    /// <returns>Whether the mixture is probably safe.</returns>
    [PublicAPI]
    public bool IsMixtureProbablySafe(GasMixture? air)
    {
        // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
        if (air == null)
            return false;

        switch (air.Pressure)
        {
            case <= Atmospherics.WarningLowPressure:
            case >= Atmospherics.WarningHighPressure:
                return false;
        }

        switch (air.Temperature)
        {
            case <= 260: // TODO ATMOS nuke these hardcoded values someday
            case >= 360:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets an enumerator for the adjacent tile mixtures of a tile on a grid.
    /// </summary>
    /// <param name="grid">The grid to get adjacent tile mixtures from.</param>
    /// <param name="tile">The tile to get adjacent mixtures for.</param>
    /// <param name="includeBlocked">Whether to include blocked adjacent tiles.</param>
    /// <param name="excite">Whether to mark the adjacent tiles as active for atmosphere processing.</param>
    /// <returns>An enumerator for the adjacent tile mixtures.</returns>
    [PublicAPI]
    public TileMixtureEnumerator GetAdjacentTileMixtures(Entity<GridAtmosphereComponent?> grid, Vector2i tile, bool includeBlocked = false, bool excite = false)
    {
        // TODO ATMOS includeBlocked and excite parameters are unhandled currently.
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return TileMixtureEnumerator.Empty;

        return !grid.Comp.Tiles.TryGetValue(tile, out var atmosTile)
            ? TileMixtureEnumerator.Empty
            : new TileMixtureEnumerator(atmosTile.AdjacentTiles);
    }

    /// <summary>
    /// Checks if the gas mixture on a tile is "probably safe".
    /// Probably safe is defined as having at least air alarm-grade safe pressure and temperature.
    /// (more than 260K, less than 360K, and between safe low and high pressure as defined in
    /// <see cref="Atmospherics.WarningLowPressure"/> and <see cref="Atmospherics.WarningHighPressure"/>)
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <param name="map">The map to check.</param>
    /// <param name="tile">The tile to check.</param>
    /// <returns>True if the tile's mixture is probably safe, false otherwise.</returns>
    [PublicAPI]
    public bool IsTileMixtureProbablySafe(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return IsMixtureProbablySafe(GetTileMixture(grid, map, tile));
    }

    /// <summary>
    /// Gets the heat capacity of the gas mixture on a tile.
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <param name="map">The map to check.</param>
    /// <param name="tile">The tile on the grid/map to check.</param>
    /// <returns>>The heat capacity of the tile's mixture, or the heat capacity of space if a mixture could not be found.</returns>
    [PublicAPI]
    public float GetTileHeatCapacity(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return GetHeatCapacity(GetTileMixture(grid, map, tile) ?? GasMixture.SpaceGas);
    }
}

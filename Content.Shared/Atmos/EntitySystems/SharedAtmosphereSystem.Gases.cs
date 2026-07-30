using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Atmos.Reactions;
using Content.Shared.CCVar;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /*
     Partial class for operations involving GasMixtures.

     Sometimes methods here are abstract because they need different client/server implementations
     due to sandboxing.
     */

    /// <summary>
    /// Cached array of molar heat capacities of the gases.
    /// </summary>
    public float[] GasMolarHeatCapacities => _gasMolarHeatCapacities;

    private float[] _gasMolarHeatCapacities = new float[Atmospherics.AdjustedNumberOfGases];

    /// <summary>
    /// Cached array of gas specific mols
    /// </summary>
    public float[] GasMolarMasses => _gasMolarMasses;

    private float[] _gasMolarMasses = new float[Atmospherics.AdjustedNumberOfGases];

    /// <summary>
    /// Mask used to determine if a gas is flammable or not.
    /// </summary>
    /// <para>This is used to quickly determine if a <see cref="GasMixture"/> contains any flammable gas.
    /// When determining flammability, the float is multiplied with the mask and then
    /// added to see if the mixture is flammable, and how many moles are considered flammable.</para>
    /// <para>This is done instead of a massive if statement of doom everywhere.</para>
    /// <example><para>Say Plasma has the <see cref="GasPrototype.IsFuel"/> bool set to true.
    /// Atmospherics will place a 1 in the spot where plasma goes in the masking array.
    /// Whenever we need to determine if a GasMixture contains fuel gases, we multiply the
    /// gas array by the mask. Fuel gases will keep their value (being multiplied by one)
    /// whereas non-fuel gases will be multiplied by zero and be zeroed out.
    /// The resulting array can be HorizontalAdded, with any value above zero indicating fuel gases.</para>
    /// <para>This works for multiple fuel gases at the same time, so it's a fairly quick way
    /// to determine if a mixture has the gases we care about.</para></example>
    protected readonly float[] GasFuelMask = new float[Atmospherics.AdjustedNumberOfGases];

    /// <summary>
    /// Mask used to determine if a gas is an oxidizer or not.
    /// <para>Used in the same way as <see cref="GasFuelMask"/>.
    /// Nothing really super special.</para>
    /// </summary>
    protected readonly float[] GasOxidizerMask = new float[Atmospherics.AdjustedNumberOfGases];

    /// <summary>
    /// Mask used to determine both fuel and oxidizer properties of a gas at the same time.
    /// Primarily used to quickly report the specific moles in a mixture that caused a flammable reaction to occur.
    /// </summary>
    protected readonly float[] GasOxidiserFuelMask = new float[Atmospherics.TotalNumberOfGases];

    public string?[] GasReagents = new string[Atmospherics.TotalNumberOfGases];
    protected readonly GasPrototype[] GasPrototypes = new GasPrototype[Atmospherics.TotalNumberOfGases];

    public virtual void InitializeGases()
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            var idx = (int)gas;
            // Log an error if the corresponding prototype isn't found
            if (!ProtoMan.TryIndex<GasPrototype>(gas.ToString(), out var gasPrototype))
            {
                Log.Error($"Failed to find corresponding {nameof(GasPrototype)} for gas ID {(int)gas} ({gas}) with expected ID \"{gas.ToString()}\". Is your prototype named correctly?");
                continue;
            }
            GasPrototypes[idx] = gasPrototype;
            GasReagents[idx] = gasPrototype.Reagent;
        }

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            /*
             As an optimization routine we pre-divide the specific heat by the heat scale here,
             so we don't have to do it every time we calculate heat capacity.
             Most usages are going to want the scaled value anyway.

             If you would like the unscaled specific heat, you'd need to multiply by HeatScale again.
             TODO ATMOS: please just make this 2 separate arrays instead of invoking multiplication every time.
             */
            _gasMolarHeatCapacities[i] = GasPrototypes[i].MolarHeatCapacity / HeatScale;
            _gasMolarMasses[i] = GasPrototypes[i].MolarMass;

            // """Mask""" built here. Used to determine if a gas is fuel/oxidizer or not decently quickly and clearly.
            GasFuelMask[i] = GasPrototypes[i].IsFuel ? 1 : 0;

            // Same for oxidizer mask.
            GasOxidizerMask[i] = GasPrototypes[i].IsOxidizer ? 1 : 0;

            // OxidiserFuel mask is just fuel and oxidizer combined, because both are required for a reaction to occur.
            GasOxidiserFuelMask[i] = GasFuelMask[i] * GasOxidizerMask[i];
        }
    }

    /// <summary>
    /// Gets only the moles that are considered a fuel and an oxidizer in a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to get the flammable moles for.</param>
    /// <param name="buffer">A buffer to write the flammable moles into. Must be the same length as the number of gases.</param>
    /// <returns>A <see cref="Span{T}"/> of moles where only the flammable and oxidizer moles are returned, and the rest are 0.</returns>
    [PublicAPI]
    public void GetFlammableMoles(GasMixture mixture, float[] buffer)
    {
        NumericsHelpers.Multiply(mixture.Moles, GasOxidiserFuelMask, buffer);
    }

    /// <summary>
    /// Determines if a <see cref="GasMixture"/> is ignitable or not.
    /// This is a combination of determining if a mixture both has oxidizer and fuel.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to determine.</param>
    /// <param name="epsilon">The minimum amount of moles at which a <see cref="GasMixture"/> is
    /// considered ignitable, for both oxidizer and fuel.</param>
    /// <returns>True if the <see cref="GasMixture"/> is ignitable, otherwise, false.</returns>
    [PublicAPI]
    public bool IsMixtureIgnitable(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        return IsMixtureFuel(mixture, epsilon) && IsMixtureOxidizer(mixture, epsilon);
    }

    /// <summary>
    /// Determines if a <see cref="GasMixture"/> has fuel gases in it or not.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to determine.</param>
    /// <param name="epsilon">The minimum amount of moles at which a <see cref="GasMixture"/>
    /// is considered fuel.</param>
    /// <returns>True if the <see cref="GasMixture"/> is fuel, otherwise, false.</returns>
    [PublicAPI]
    public abstract bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon);

    /// <summary>
    /// Determines if a <see cref="GasMixture"/> has oxidizer gases in it or not.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to determine.</param>
    /// <param name="epsilon">The minimum amount of moles at which a <see cref="GasMixture"/>
    /// is considered an oxidizer.</param>
    /// <returns>True if the <see cref="GasMixture"/> is an oxidizer, otherwise, false.</returns>
    [PublicAPI]
    public abstract bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon);

    /// <summary>
    /// Calculates the heat capacity for a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to calculate the heat capacity for.</param>
    /// <param name="applyScaling">Whether to apply the heat capacity scaling factor.
    /// This is an extremely important boolean to consider or else you will get heat transfer wrong.
    /// See <see cref="CCVars.AtmosHeatScale"/> for more info.</param>
    /// <returns>The heat capacity of the <see cref="GasMixture"/>.</returns>
    [PublicAPI]
    public float GetHeatCapacity(GasMixture mixture, bool applyScaling)
    {
        var scale = GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);

        // By default GetHeatCapacityCalculation() has the heat-scale divisor pre-applied.
        // So if we want the un-scaled heat capacity, we have to multiply by the scale.
        return applyScaling ? scale : scale * HeatScale;
    }

    /// <summary>
    /// Calculates the thermal energy for a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to calculate the thermal
    /// energy of.</param>
    /// <returns>The <see cref="GasMixture"/>'s thermal energy in joules.</returns>
    [PublicAPI]
    public float GetThermalEnergy(GasMixture mixture)
    {
        return mixture.Temperature * GetHeatCapacity(mixture);
    }

    /// <summary>
    /// Calculates the thermal energy for a gas mixture,
    /// using a provided cached heat capacity value.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to calculate the thermal energy of.</param>
    /// <param name="cachedHeatCapacity">A cached heat capacity value for the gas mixture,
    /// to avoid redundant heat capacity calculations.</param>
    /// <returns>The <see cref="GasMixture"/>'s thermal energy in joules.</returns>
    [PublicAPI]
    public float GetThermalEnergy(GasMixture mixture, float cachedHeatCapacity)
    {
        return mixture.Temperature * cachedHeatCapacity;
    }

    /// <summary>
    /// Merges one <see cref="GasMixture"/> into another, modifying the receiver.
    /// </summary>
    /// <param name="receiver">The <see cref="GasMixture"/> to merge into. This will be modified.</param>
    /// <param name="giver">The <see cref="GasMixture"/> to merge from. This will not be modified.</param>
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
    /// Performs reactions for a given gas mixture on an optional holder.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to perform reactions on.</param>
    /// <param name="holder"><see cref="IGasMixtureHolder"/> that holds the <see cref="GasMixture"/>.
    /// used by Atmospherics to determine locality for certain reaction effects.</param>
    /// <returns>The <see cref="ReactionResult"/> of the reactions performed.</returns>
    [PublicAPI]
    public abstract ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder);

    /// <summary>
    /// Gets the heat capacity for a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to calculate the heat capacity for.</param>
    /// <returns>The heat capacity of the <see cref="GasMixture"/>.</returns>
    /// <remarks>Note that the heat capacity of the mixture may be slightly different from
    /// "real life" as we intentionally fake a heat capacity for space in <see cref="Atmospherics.SpaceHeatCapacity"/>
    /// in order to allow Atmospherics to cool down space.</remarks>
    protected float GetHeatCapacity(GasMixture mixture)
    {
        return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
    }

    /// <summary>
    /// Gets the mass of a given <see cref="GasMixture"/>
    /// </summary>
    /// <param name="mix">The <see cref="GasMixture"/> in question</param>
    /// <returns>Returns the volume in kilograms.</returns>
    [PublicAPI]
    public abstract float GetMass(GasMixture mix);

    /// <inheritdoc cref="GetMass(GasMixture)"/>
    [PublicAPI]
    public abstract float GetMass(float[] moles);

    /// <summary>
    /// Calculates the amount of volume transferred from one gas mixture to another over time based on flow rate.
    /// <see cref="GetFlowRate(GasMixture,GasMixture,float,float)"/>
    /// </summary>
    /// <param name="mix1">A <see cref="GasMixture"/></param>
    /// <param name="mix2">Another <see cref="GasMixture"/></param>
    /// <param name="area">The area of transfer, in square meters. One tile of movement is about one square meter.</param>
    /// <param name="dt">delta time, or how much time is passing/has passed.</param>
    /// <param name="c">Discharge coefficient. An abstract modifier for friction and turbulence.</param>
    /// <returns>
    /// The volume of gas being moved over dt in Litres.
    /// If the value is positive it's in the direction of mix1->mix2,
    /// If it's negative it's in the direction of mix2 -> mix1
    /// </returns>
    /// <remarks>I'm assuming C is always 1 because I'm lazy, you can precalculate it and pass it with the area if you really care.</remarks>
    [PublicAPI]
    public double GetFlowVolume(GasMixture mix1, GasMixture mix2, float area, float dt, float c = 1f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dt);
        return dt * GetFlowRate(mix1, mix2, area, c);
    }

    /// <see cref="GetFlowVolume(GasMixture,GasMixture,float,float,float)"/>
    [PublicAPI]
    public double GetFlowVolume(GasMixture mix1, float deltaP, float area, float dt, float c = 1f)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dt);
        return dt * GetFlowRate(mix1, deltaP, area, c);
    }

    /// <summary>
    /// Calculates the volumetric flow rate between two gas mixtures.
    /// </summary>
    /// <param name="mix1">A <see cref="GasMixture"/></param>
    /// <param name="mix2">Another <see cref="GasMixture"/></param>
    /// <param name="area">The area of transfer, in square meters. One tile of movement is about one square meter.</param>
    /// <param name="c">Discharge coefficient. An abstract modifier for friction and turbulence.</param>
    /// <returns>
    /// The volume of gas being moved in Litres / Second.
    /// If the value is positive it's in the direction of mix1->mix2,
    /// If it's negative it's in the direction of mix2 -> mix1
    /// </returns>
    /// <remarks>I'm assuming C is always 1 because I'm lazy, you can precalculate it and pass it with the area if you really care.</remarks>
    [PublicAPI]
    public double GetFlowRate(GasMixture mix1, GasMixture mix2, float area, float c = 1f)
    {
        /*
            Q = C × A × √(2 × ΔP / ρ)
            Q is the volumetric airflow rate
            C is the discharge coefficient
            A is the cross-sectional area
            ΔP is the measured pressure difference
            ρ is the air density, adjusted for environmental conditions.
            We can break this up into Q = A × V where V is the velocity of the gas.
         */
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(area);
        return area * GetFlowVelocity(mix1, mix2, c);
    }

    /// <inhereitdoc cref="GetFlowRate(GasMixture,GasMixture,float, float)"/>
    [PublicAPI]
    public double GetFlowRate(GasMixture mix1, float deltaP, float area, float c = 1f)
    {
        /*
            Q = C × A × √(2 × ΔP / ρ)
            Q is the volumetric airflow rate
            C is the discharge coefficient
            A is the cross-sectional area
            ΔP is the measured pressure difference
            ρ is the air density, adjusted for environmental conditions.
            We can break this up into Q = A × V where V is the velocity of the gas.
         */
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(area);
        return area * GetFlowVelocity(mix1, deltaP, c);
    }

    /// <summary>
    /// Calculates the flow velocity between two gas mixtures using Q = C × A × √(2 × ΔP / ρ) but without the A (area)
    /// Useful for determining flow rate, or how fast a gas is moving.
    /// </summary>
    /// <param name="mix1">A <see cref="GasMixture"/></param>
    /// <param name="mix2">Another <see cref="GasMixture"/></param>
    /// <param name="c">Discharge coefficient. An abstract modifier for friction and turbulence.</param>
    /// <returns>
    /// The velocity of gas movement between two mixtures in Meters / Second.
    /// If the value is positive it's in the direction of mix1->mix2,
    /// If it's negative it's in the direction of mix2 -> mix1
    /// </returns>
    [PublicAPI]
    public double GetFlowVelocity(GasMixture mix1, GasMixture mix2, float c = 1f)
    {
        if (mix1.Pressure > mix2.Pressure)
            return GetFlowVelocity(mix1, mix1.Pressure - mix2.Pressure, c);

        return -GetFlowVelocity(mix2, mix2.Pressure - mix1.Pressure, c);
    }

    /// <summary>
    /// Calculates the flow velocity between a gas mixture given a pressure differential.
    /// </summary>
    /// <param name="mix1">The mixture which is being allowed to flow</param>
    /// <param name="deltaP">The difference in pressure between this mixture and where it's flowing to</param>
    /// <param name="c">Discharge coefficient. An abstract modifier for friction and turbulence.</param>
    /// <returns>
    /// The velocity of the gas leaving our mixture in Meters / Second.
    /// </returns>
    [PublicAPI]
    public double GetFlowVelocity(GasMixture mix1, float deltaP, float c = 1f)
    {
        /*
            V = C × √(2 × ΔP / ρ)
            V is the velocity of our gas
            C is the discharge coefficient
            ΔP is the measured pressure difference
            ρ is the air density, adjusted for environmental conditions.
            Density is equivalent to Mass / Volume, so we invert that to divide by density.
         */
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deltaP);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(c);
        return c * Math.Sqrt(2 * deltaP * mix1.Volume / GetMass(mix1));
    }

    /// <summary>
    /// Lets a volume of gas flow throw a constrained area into another volume of gas over a period of time.
    /// </summary>
    /// <param name="mixture">Gas volume that is discharging some of its gas.</param>
    /// <param name="output">Gas volume that is receiving the discharge.</param>
    /// <param name="dt">Time that the discharge occurs in seconds, should be as small as possible since it doesn't use calculus</param>
    /// <param name="area">Area that our gas is traveling through in m^2, the larger the area the bigger the transfer.
    /// Default of 2m^2 since that's the area of a single face of an atmos tile.</param>
    [PublicAPI]
    public void FlowGas(GasMixture mixture, GasMixture? output, float dt, float area)
    {
        FlowGas(mixture, output, mixture.Pressure, dt, area);
    }

    /// <inheritdoc cref="FlowGas(GasMixture,GasMixture?,float,float)"/>
    [PublicAPI]
    public void FlowGas(GasMixture mixture, GasMixture? output, float pressure, float dt, float area)
    {
        if (output == null)
        {
            FlowGas(mixture, pressure, dt, area);
            return;
        }

        pressure = Math.Min(pressure, mixture.Pressure - output.Pressure);
        var removed = FlowGas(mixture, pressure, dt, area);

        if (removed == null)
            return;

        Merge(output, removed);
    }

    /// <summary>
    /// Lets a volume of gas flow through constrained area at a constrained pressure delta.
    /// </summary>
    /// <param name="mixture">Mixture of gas that is currently flowing</param>
    /// <param name="deltaP">Pressure our gas is able to flow at.</param>
    /// <param name="dt">Time that the discharge occurs in seconds, should be as small as possible since it doesn't use calculus</param>
    /// <param name="area">Area that our gas is traveling through in m^2, the larger the area the bigger the transfer.
    /// Default of 2m^2 since that's the area of a single face of an atmos tile.</param>
    /// <returns></returns>
    [PublicAPI]
    public GasMixture? FlowGas(GasMixture mixture, float deltaP, float dt, float area = 2f)
    {
        if (deltaP <= 0)
            return null;

        return ReleaseGasAt(mixture, (float)GetFlowVolume(mixture, deltaP, area, dt), mixture.Pressure);
    }

    /// <summary>
    /// Releases some volume of a gas mixture at a specified pressure.
    /// </summary>
    /// <param name="mixture">Mixture which is releasing gas.</param>
    /// <param name="output">Optional Mixture to receive gas</param>
    /// <param name="volume">Volume we are releasing</param>
    /// <param name="targetPressure">Pressure of the released volume.</param>
    [PublicAPI]
    public void ReleaseGasAt(GasMixture mixture, GasMixture? output, float volume, float targetPressure)
    {
        if (output == null)
        {
            ReleaseGasAt(mixture, volume, targetPressure);
            return;
        }

        targetPressure = Math.Min(targetPressure, mixture.Pressure - output.Pressure);

        if (targetPressure <= 0)
            return;

        var molesNeeded = Math.Min(targetPressure * volume / (Atmospherics.R * mixture.Temperature),
            MolesToEqualizePressure(mixture, output));

        var removed = mixture.Remove(molesNeeded);

        Merge(mixture, removed);
    }

    /// <inhereitdoc cref="ReleaseGasAt(GasMixture,GasMixture?,float,float)"/>
    [PublicAPI]
    public GasMixture? ReleaseGasAt(GasMixture mixture, float volume, float targetPressure)
    {
        if (targetPressure <= 0)
            return null;

        targetPressure = Math.Min(targetPressure, mixture.Pressure);

        return RemoveVolumeAtPressure(mixture, volume, targetPressure);
    }

    /// <summary>
    /// Removes a specified volume of gas from a mixture, at a specific pressure.
    /// </summary>
    /// <param name="mixture">mixture of gas</param>
    /// <param name="volume">volume we're attempting to remove</param>
    /// <param name="pressure">pressure that volume will be removed at.</param>
    public GasMixture RemoveVolumeAtPressure(GasMixture mixture, float volume, float pressure)
    {
        var molesNeeded = pressure * volume / (Atmospherics.R * mixture.Temperature);
        return mixture.Remove(molesNeeded);
    }

    /// <summary>
    /// Gets the heat capacity for a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="moles">The moles array of the <see cref="GasMixture"/></param>
    /// <param name="space">Whether this <see cref="GasMixture"/> represents space,
    /// and thus experiences space-specific mechanics (we cheat and make it a bit cooler).
    /// See <see cref="Atmospherics.SpaceHeatCapacity"/>.</param>
    /// <returns>The heat capacity of the <see cref="GasMixture"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract float GetHeatCapacityCalculation(float[] moles, bool space);


    /// <summary>
    /// Calculates the moles that must be transferred from
    /// <see cref="gasMixture1"/> to <see cref="gasMixture2"/> to equalize pressure.
    /// </summary>
    public float MolesToEqualizePressure(GasMixture gasMixture1, GasMixture gasMixture2)
    {
        return gasMixture1.TotalMoles * FractionToEqualizePressure(gasMixture1, gasMixture2);
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
    public static float MolesToPressureThreshold(GasMixture gasMixture, float targetPressure)
    {
        // Kid named PV = nRT.
        return gasMixture.TotalMoles -
               targetPressure * gasMixture.Volume / (Atmospherics.R * gasMixture.Temperature);
    }
}

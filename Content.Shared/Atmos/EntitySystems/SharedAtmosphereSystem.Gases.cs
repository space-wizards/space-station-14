using System.Diagnostics;
using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Prototypes;
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
    /// Cached array of gas specific heats
    /// </summary>
    public float[] GasSpecificHeats => _gasSpecificHeats;

    private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

    /// <summary>
    /// Cached array of gas specific mols
    /// </summary>
    public float[] GasMolarMasses => _gasMolarMasses;

    private float[] _gasMolarMasses = new float[Atmospherics.TotalNumberOfGases];

    public string?[] GasReagents = new string[Atmospherics.TotalNumberOfGases];
    protected readonly GasPrototype[] GasPrototypes = new GasPrototype[Atmospherics.TotalNumberOfGases];

    public virtual void InitializeGases()
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            var idx = (int)gas;
            // Log an error if the corresponding prototype isn't found
            if (!_prototypeManager.TryIndex<GasPrototype>(gas.ToString(), out var gasPrototype))
            {
                Log.Error(
                    $"Failed to find corresponding {nameof(GasPrototype)} for gas ID {(int)gas} ({gas}) with expected ID \"{gas.ToString()}\". Is your prototype named correctly?");
                continue;
            }

            GasPrototypes[idx] = gasPrototype;
            GasReagents[idx] = gasPrototype.Reagent;
        }

        Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));
        Array.Resize(ref _gasMolarMasses, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

        for (var i = 0; i < GasPrototypes.Length; i++)
        {
            /*
             As an optimization routine we pre-divide the specific heat by the heat scale here,
             so we don't have to do it every time we calculate heat capacity.
             Most usages are going to want the scaled value anyway.

             If you would like the unscaled specific heat, you'd need to multiply by HeatScale again.
             TODO ATMOS: please just make this 2 separate arrays instead of invoking multiplication every time.
             */
            _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat / HeatScale;
            _gasMolarMasses[i] = GasPrototypes[i].MolarMass;
        }
    }

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
    /// Calculates the flow volume between two gas mixtures.
    /// Q = C × A × √(2 × ΔP / ρ)
    /// Q is the volumetric airflow rate
    /// C is the discharge coefficient
    /// A is the cross-sectional area
    /// ΔP is the measured pressure difference
    /// ρ is the air density, adjusted for environmental conditions.
    /// </summary>
    /// <param name="mix1">A <see cref="GasMixture"/></param>
    /// <param name="mix2">Another <see cref="GasMixture"/></param>
    /// <param name="area">The area of transfer, in square meters. One tile of movement is about one square meter.</param>
    /// <returns>
    /// The volume of gas being moved in Litres / Second.
    /// If the value is positive it's in the direction of mix1->mix2,
    /// If it's negative it's in the direction of mix2 -> mix1
    /// </returns>
    /// <remarks>I'm assuming C is always 1 because I'm lazy, you can precalculate it and pass it with the area if you really care.</remarks>
    [PublicAPI]
    public double GetFlowVolume(GasMixture mix1, GasMixture mix2, float area)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(area);
        return area * GetFlowVelocity(mix1, mix2);
    }

    /// <inhereitdoc cref="GetFlowVolume(GasMixture,GasMixture,float)"/>
    [PublicAPI]
    public double GetFlowVolume(GasMixture mix1, float deltaP, float area)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(area);
        return area * GetFlowVelocity(mix1, deltaP);
    }

    /// <summary>
    /// Calculates the flow velocity between two gas mixtures.
    /// Useful for determining flow rate.
    /// </summary>
    /// <param name="mix1">A <see cref="GasMixture"/></param>
    /// <param name="mix2">Another <see cref="GasMixture"/></param>
    /// <returns>
    /// The velocity of gas movement between two mixtures in Meters / Second.
    /// If the value is positive it's in the direction of mix1->mix2,
    /// If it's negative it's in the direction of mix2 -> mix1
    /// </returns>
    [PublicAPI]
    public double GetFlowVelocity(GasMixture mix1, GasMixture mix2)
    {
        if (mix1.Pressure > mix2.Pressure)
            return GetFlowVelocity(mix1, mix1.Pressure - mix2.Pressure);

        return -GetFlowVelocity(mix2, mix2.Pressure - mix1.Pressure);
    }

    /// <summary>
    /// Calculates the flow velocity between a gas mixture given a pressure differential.
    /// </summary>
    /// <param name="mix1">The mixture which is being allowed to flow</param>
    /// <param name="deltaP">The difference in pressure between this mixture and where it's flowing to</param>
    /// <returns>
    /// The velocity of the gas leaving our mixture in Meters / Second.
    /// </returns>
    [PublicAPI]
    public double GetFlowVelocity(GasMixture mix1, float deltaP)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(deltaP);
        if (deltaP == 0)
            return 0;

        return Math.Sqrt(2 * deltaP * mix1.Volume / GetMass(mix1));
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
}

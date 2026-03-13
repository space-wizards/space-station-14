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
    /// Cached array of gas specific heats.
    /// </summary>
    public float[] GasSpecificHeats => _gasSpecificHeats;
    private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

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
            if (!_prototypeManager.TryIndex<GasPrototype>(gas.ToString(), out var gasPrototype))
            {
                Log.Error($"Failed to find corresponding {nameof(GasPrototype)} for gas ID {(int)gas} ({gas}) with expected ID \"{gas.ToString()}\". Is your prototype named correctly?");
                continue;
            }
            GasPrototypes[idx] = gasPrototype;
            GasReagents[idx] = gasPrototype.Reagent;
        }

        Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

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
    public bool IsMixtureIgnitable(GasMixture mixture, float epsilon = 0.001f)
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
    public abstract bool IsMixtureFuel(GasMixture mixture, float epsilon = 0.001f);

    /// <summary>
    /// Determines if a <see cref="GasMixture"/> has oxidizer gases in it or not.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to determine.</param>
    /// <param name="epsilon">The minimum amount of moles at which a <see cref="GasMixture"/>
    /// is considered an oxidizer.</param>
    /// <returns>True if the <see cref="GasMixture"/> is an oxidizer, otherwise, false.</returns>
    [PublicAPI]
    public abstract bool IsMixtureOxidizer(GasMixture mixture, float epsilon = 0.001f);

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

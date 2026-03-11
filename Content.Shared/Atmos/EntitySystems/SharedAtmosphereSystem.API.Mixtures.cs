using Content.Shared.CCVar;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
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
}

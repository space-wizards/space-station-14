using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Partial class for operations involving GasMixtures.

     Any method that is overridden here is usually because the server-sided implementation contains
     code that would escape sandbox. As such these methods are overridden here with a safe
     implementation.
     */

    /// <inheritdoc/>
    /// <remarks>No-op on client as reactions aren't entirely in shared.
    /// Don't call it. Smile.</remarks>
    public override ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder)
    {
        // Reactions don't work on client so don't even try.
        throw new NotImplementedException();
    }

    public override bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        var tmp = new float[Atmospherics.AdjustedNumberOfGases];
        TensorPrimitives.Multiply(mixture.Moles, GasFuelMask, tmp);
        return TensorPrimitives.Sum(tmp) > epsilon;
    }

    public override bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        var tmp = new float[Atmospherics.AdjustedNumberOfGases];
        TensorPrimitives.Multiply(mixture.Moles, GasOxidizerMask, tmp);
        return TensorPrimitives.Sum(tmp) > epsilon;
    }

    public override float GetMass(GasMixture mix)
    {
        return GetMass(mix.Moles);
    }

    public override float GetMass(float[] moles)
    {
        var tmp = new float[moles.Length];
        TensorPrimitives.Multiply(moles, GasMolarMasses, tmp);

        // Conversion of grams to kilograms.
        return TensorPrimitives.Sum(tmp) * Atmospherics.gToKg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetHeatCapacityCalculation(float[] moles, bool space)
    {
        // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
        if (space && MathHelper.CloseTo(TensorPrimitives.Sum(moles), 0f))
        {
            return Atmospherics.SpaceHeatCapacity;
        }

        // explicit stackalloc call is banned on client tragically.
        // the JIT does not stackalloc this during runtime,
        // though this isnt the hottest code path so it should be fine
        // the gc can eat a little as a treat
        var tmp = new float[moles.Length];
        TensorPrimitives.Multiply(moles, GasMolarHeatCapacities, tmp);
        // Adjust heat capacity by speedup, because this is primarily what
        // determines how quickly gases heat up/cool.
        return MathF.Max(TensorPrimitives.Sum(tmp), Atmospherics.MinimumHeatCapacity);
    }
}

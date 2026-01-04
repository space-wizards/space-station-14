using System.Runtime.CompilerServices;
using Content.Shared.Atmos;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Partial class for operations involving GasMixtures.

     Any method that is overridden here is usually because the server-sided implementation contains
     code that would escape sandbox. As such these methods are overridden here with a safe
     implementation.
     */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetHeatCapacityCalculation(float[] moles, bool space)
    {
        // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
        if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
        {
            return Atmospherics.SpaceHeatCapacity;
        }

        // stackalloc is banned on client tragically.
        // in .NET 9/10 the JIT will probably just stackalloc this anyway because it doesn't escape,
        // especially considering that NumericsHelpers is all inlined.
        var tmp = new float[moles.Length];
        NumericsHelpers.Multiply(moles, GasSpecificHeats, tmp);
        // Adjust heat capacity by speedup, because this is primarily what
        // determines how quickly gases heat up/cool.
        return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
    }
}

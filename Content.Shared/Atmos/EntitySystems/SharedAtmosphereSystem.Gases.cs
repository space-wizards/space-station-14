using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Prototypes;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    /*
     Partial class for operations involving GasMixtures.

     Sometimes methods here are abstract because they need different client/server implementations
     due to sandboxing.
     */

    private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

    /// <summary>
    ///     Cached array of gas specific heats.
    /// </summary>
    public float[] GasSpecificHeats => _gasSpecificHeats;

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
            _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat / HeatScale;
        }
    }

    /// <summary>
    ///     Calculates the heat capacity for a gas mixture.
    /// </summary>
    /// <param name="mixture">The mixture whose heat capacity should be calculated</param>
    /// <param name="applyScaling"> Whether the internal heat capacity scaling should be applied. This should not be
    /// used outside of atmospheric related heat transfer.</param>
    /// <returns></returns>
    public float GetHeatCapacity(GasMixture mixture, bool applyScaling)
    {
        var scale = GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);

        // By default GetHeatCapacityCalculation() has the heat-scale divisor pre-applied.
        // So if we want the un-scaled heat capacity, we have to multiply by the scale.
        return applyScaling ? scale : scale * HeatScale;
    }

    protected float GetHeatCapacity(GasMixture mixture)
    {
        return GetHeatCapacityCalculation(mixture.Moles, mixture.Immutable);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract float GetHeatCapacityCalculation(float[] moles, bool space);
}

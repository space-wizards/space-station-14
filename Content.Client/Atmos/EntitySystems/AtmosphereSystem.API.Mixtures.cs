using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [PublicAPI]
    public override bool IsMixtureFuel(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        var tmp = new float[Atmospherics.AdjustedNumberOfGases];
        NumericsHelpers.Multiply(mixture.Moles, GasFuelMask, tmp);
        return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
    }

    [PublicAPI]
    public override bool IsMixtureOxidizer(GasMixture mixture, float epsilon = Atmospherics.Epsilon)
    {
        var tmp = new float[Atmospherics.AdjustedNumberOfGases];
        NumericsHelpers.Multiply(mixture.Moles, GasOxidizerMask, tmp);
        return NumericsHelpers.HorizontalAdd(tmp) > epsilon;
    }
}

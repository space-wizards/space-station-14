using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Body.Circulatory
{
    public partial class BloodstreamComponentData
    {
        [CustomYamlField("air")] public GasMixture Air = new GasMixture(6)
            {Temperature = Atmospherics.NormalBodyTemperature};
    }
}

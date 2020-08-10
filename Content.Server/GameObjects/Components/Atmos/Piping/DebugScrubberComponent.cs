using Content.Server.Atmos;

namespace Content.Server.GameObjects.Components.Atmos
{
    public class DebugScrubberComponent : BaseScrubberComponent
    {
        public override string Name => "DebugScrubber";

        protected override void ScrubGas(GasMixture inletGas, GasMixture outletGas, float frameTime)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

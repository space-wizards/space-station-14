using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Placeholder example of scrubber functionality.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BaseScrubberComponent))]
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

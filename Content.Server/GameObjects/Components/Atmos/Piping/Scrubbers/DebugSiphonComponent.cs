using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Placeholder example of scrubber functionality.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BaseSiphonComponent))]
    public class DebugSiphonComponent : BaseSiphonComponent
    {
        public override string Name => "DebugSiphon";

        protected override void ScrubGas(GasMixture inletGas, GasMixture outletGas)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

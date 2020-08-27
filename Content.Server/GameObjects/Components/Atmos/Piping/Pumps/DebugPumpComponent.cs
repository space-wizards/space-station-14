using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///  Placeholder example of pump functionality.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BasePumpComponent))]
    public class DebugPumpComponent : BasePumpComponent
    {
        public override string Name => "DebugPump";

        protected override void PumpGas(GasMixture inletGas, GasMixture outletGas)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

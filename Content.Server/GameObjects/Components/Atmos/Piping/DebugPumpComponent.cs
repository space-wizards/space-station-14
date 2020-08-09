using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    [ComponentReference(typeof(BasePumpComponent))]
    public class DebugPumpComponent : BasePumpComponent
    {
        public override string Name => "DebugPump";

        protected override void PumpGas(GasMixture inletGas, GasMixture outletGas, float frameTime)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

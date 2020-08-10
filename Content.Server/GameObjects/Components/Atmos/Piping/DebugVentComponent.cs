using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    [RegisterComponent]
    [ComponentReference(typeof(BaseVentComponent))]
    public class DebugVentComponent : BaseVentComponent
    {
        public override string Name => "DebugVent";

        protected override void VentGas(GasMixture inletGas, GasMixture outletGas, float frameTime)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

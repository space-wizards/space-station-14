using Content.Server.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Vents
{
    /// <summary>
    ///     Placeholder example of vent functionality.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BaseVentComponent))]
    public class DebugVentComponent : BaseVentComponent
    {
        public override string Name => "DebugVent";

        protected override void VentGas(GasMixture inletGas, GasMixture outletGas)
        {
            outletGas.Merge(inletGas);
            inletGas.Clear();
        }
    }
}

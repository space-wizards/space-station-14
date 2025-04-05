using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Toggleable;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    public sealed partial class GasFilterComponent : Component
    {
        [Access(typeof(GasFilterSystem))]
        public ToggleableComponent ToggleableComponent;

        /// <summary>
        ///     The default Enabled value for this comp's ToggleableComponent. Only used on init.
        /// </summary>
        [DataField("enabled")]
        public bool DefaultEnabled = false;

        [DataField("inlet")]
        public string InletName = "inlet";

        [DataField("filter")]
        public string FilterName = "filter";

        [DataField("outlet")]
        public string OutletName = "outlet";

        [DataField]
        public float TransferRate = Atmospherics.MaxTransferRate;

        [DataField]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        [DataField]
        public Gas? FilteredGas;
    }
}

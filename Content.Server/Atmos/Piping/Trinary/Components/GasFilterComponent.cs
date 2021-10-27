using System;
using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    public class GasFilterComponent : Component
    {
        public override string Name => "GasFilter";

        private bool _enabled = true;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdateFilterUIEvent(Owner.Uid));
            }
        }
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("filter")]
        public string FilterName { get; set; } = "filter";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        private float _transferRate = Atmospherics.MaxTransferRate;
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set
            {
                _transferRate = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdateFilterUIEvent(Owner.Uid));
            }
        }
        private Gas? _filteredGas;
        [ViewVariables(VVAccess.ReadWrite)]
        public Gas? FilteredGas
        {
            get => _filteredGas;
            set
            {
                _filteredGas = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdateFilterUIEvent(Owner.Uid));
            }
        }
    }
}

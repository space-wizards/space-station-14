using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.VendingMachines
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class VendingCategoryComponent : Component
    {
        [DataField]
        public List<ProtoId<ItemCategoryPrototype>> Categories = [];
    }
}

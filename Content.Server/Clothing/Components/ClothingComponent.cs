using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [Virtual]
    public class ItemComponent : SharedItemComponent{}

    [RegisterComponent]
    [NetworkedComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public sealed class ClothingComponent : ItemComponent
    {
        [DataField("HeatResistance")]
        private int _heatResistance = 323;

        [ViewVariables(VVAccess.ReadWrite)]
        public int HeatResistance => _heatResistance;
    }
}

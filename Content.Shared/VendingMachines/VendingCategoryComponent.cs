using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.VendingMachines
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class VendingCategoryComponent : Component
    {
        /// <summary>
        /// Prototypes to be visible when a given category is selected
        /// </summary>
        [DataField]
        public Dictionary<string, HashSet<EntProtoId>> Categories;

        /// <summary>
        /// Icons to be displayed on the corresponding category's button
        /// </summary>
        [DataField]
        public Dictionary<string, EntProtoId> Icons = [];

        /// <summary>
        /// Names of categories, shows when hovered over the buttons
        /// </summary>
        [DataField]
        public Dictionary<string, LocId> Names = [];
    }
}

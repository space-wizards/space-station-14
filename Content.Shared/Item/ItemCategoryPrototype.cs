using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item
{
    [Prototype]
    public sealed partial class ItemCategoryPrototype : IPrototype
    {
        [ViewVariables, IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        /// Prototypes included in a category
        /// </summary>
        [DataField]
        public EntityWhitelist Whitelist = default!;

        /// <summary>
        /// Icon corresponding to the category
        /// </summary>
        [DataField]
        public EntProtoId? Icon;

        /// <summary>
        /// Name of category
        /// </summary>
        [DataField]
        public LocId Name = "";
    }
}

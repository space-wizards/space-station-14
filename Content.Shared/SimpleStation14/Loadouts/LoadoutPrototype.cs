using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.SimpleStation14.Loadouts
{
    /// <summary>
    ///     Describes a loadout.
    /// </summary>
    [Prototype("loadout")]
    public sealed class LoadoutPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this loadout.
        /// </summary>
        [DataField("name")]
        public string Name { get; private set; } = "";

        /// <summary>
        ///     The description of this loadout.
        /// </summary>
        [DataField("description")]
        public string? Description { get; private set; }

        /// <summary>
        ///     Need one of these jobs to give loadout.
        /// </summary>
        [DataField("jobWhitelist")]
        public List<string>? JobWhitelist { get; private set; }

        /// <summary>
        ///     Need none of these jobs to give loadout.
        /// </summary>
        [DataField("jobBlacklist")]
        public List<string>? JobBlacklist { get; private set; }

        /// <summary>
        ///     Which tab category to put this under.
        /// </summary>
        [DataField("category")]
        public string Category { get; private set; } = "Uncategorized";

        /// <summary>
        ///     The point cost of this loadout.
        /// </summary>
        [DataField("cost")]
        public int Cost = 1;

        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS NOT valid for.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS valid for. (hence, a blacklist)
        /// </summary>
        [DataField("blacklist")]
        public EntityWhitelist? Blacklist;

        /// <summary>
        ///     Should this item override other items in the same slot?
        /// </summary>
        [DataField("exclusive")]
        public bool Exclusive = false;

        [DataField("item")]
        public string? Item;
    }
}

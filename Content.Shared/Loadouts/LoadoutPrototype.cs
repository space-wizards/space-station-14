using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Loadouts
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
        public string Name = "";

        /// <summary>
        ///     The description of this loadout.
        /// </summary>
        [DataField("description")]
        public string? Description;

        /// <summary>
        ///     Which tab category to put this under.
        /// </summary>
        [DataField("category")]
        public string Category = "Uncategorized";

        /// <summary>
        ///     The item to give.
        /// </summary>
        [DataField("item")]
        public string? Item;


        /// <summary>
        ///     The point cost of this loadout.
        /// </summary>
        [DataField("cost")]
        public int Cost = 1;

        /// <summary>
        ///     Should this item override other items in the same slot?
        /// </summary>
        [DataField("exclusive")]
        public bool Exclusive;


        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS NOT valid for.
        /// </summary>
        [DataField("entityWhitelist")]
        public EntityWhitelist? EntityWhitelist;

        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS valid for. (hence, a blacklist)
        /// </summary>
        [DataField("entityBlacklist")]
        public EntityWhitelist? EntityBlacklist;

        /// <summary>
        ///     Need one of these jobs to give loadout.
        /// </summary>
        [DataField("jobWhitelist")]
        public List<string>? JobWhitelist;

        /// <summary>
        ///     Need none of these jobs to give loadout.
        /// </summary>
        [DataField("jobBlacklist")]
        public List<string>? JobBlacklist;

        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS NOT valid for.
        /// </summary>
        [DataField("speciesWhitelist")]
        public List<string>? SpeciesWhitelist;

        /// <summary>
        ///     Don't apply this loadout to entities this whitelist IS valid for. (hence, a blacklist)
        /// </summary>
        [DataField("speciesBlacklist")]
        public List<string>? SpeciesBlacklist;
    }
}

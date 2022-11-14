using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using static Robust.Shared.Prototypes.EntityPrototype; // don't worry about it

namespace Content.Shared.Traits
{
    /// <summary>
    ///     Describes a trait.
    /// </summary>
    [Prototype("trait")]
    public sealed class TraitPrototype : IPrototype
    {
        private string _name = string.Empty;
        private string? _description;

        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this trait.
        /// </summary>
        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        /// <summary>
        ///     The description of this trait.
        /// </summary>
        [DataField("description")]
        public string? Description
        {
            get => _description;
            private set => _description = value is null ? null : Loc.GetString(value);
        }

        /// <summary>
        ///     Don't apply this trait to entities this whitelist IS NOT valid for.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        /// <summary>
        ///     Don't apply this trait to entities this whitelist IS valid for. (hence, a blacklist)
        /// </summary>
        [DataField("blacklist")]
        public EntityWhitelist? Blacklist;

        /// <summary>
        ///     The components that get added to the player, when they pick this trait.
        /// </summary>
        [DataField("components")]
        public ComponentRegistry Components { get; } = default!;
    }
}

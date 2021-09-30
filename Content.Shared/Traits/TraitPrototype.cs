using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Traits
{
    /// <summary>
    ///     Describes information for a single trait on the character.
    /// </summary>
    [Prototype("trait")]
    public class TraitPrototype : IPrototype
    {
        private string _name = string.Empty;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <summary>
        ///     The name of this trait as displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     The description of this trait as displayed to players.
        /// </summary>
        [DataField("description")]
        public string Description { get; } = string.Empty;
        
        /// <summary>
        ///     Whether or not the player can set the trait in trait preferences. As some are obtainable in game only.
        /// </summary>
        [DataField("setPreference")]
        public bool SetPreference { get; private set; }
    }
}

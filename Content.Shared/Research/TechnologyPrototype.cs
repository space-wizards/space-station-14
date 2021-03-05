#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Research
{
    [NetSerializable, Serializable, Prototype("technology")]
    public class TechnologyPrototype : IPrototype
    {
        /// <summary>
        ///     The ID of this technology prototype.
        /// </summary>
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

        /// <summary>
        ///     The name this technology will have on user interfaces.
        /// </summary>
        [ViewVariables]
        [field: DataField("name")]
        public string Name { get; } = string.Empty;

        /// <summary>
        ///     An icon that represent this technology.
        /// </summary>
        [field: DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     A short description of the technology.
        /// </summary>
        [ViewVariables]
        [field: DataField("description")]
        public string Description { get; } = string.Empty;

        /// <summary>
        ///    The required research points to unlock this technology.
        /// </summary>
        [ViewVariables]
        [field: DataField("requiredPoints")]
        public int RequiredPoints { get; }

        /// <summary>
        ///     A list of technology IDs required to unlock this technology.
        /// </summary>
        [ViewVariables]
        [field: DataField("requiredTechnologies")]
        public List<string> RequiredTechnologies { get; } = new();

        /// <summary>
        ///     A list of recipe IDs this technology unlocks.
        /// </summary>
        [ViewVariables]
        [field: DataField("unlockedRecipes")]
        public List<string> UnlockedRecipes { get; } = new();
    }
}

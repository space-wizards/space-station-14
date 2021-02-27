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
        [DataField("name")]
        private string _name;

        [DataField("icon")]
        private SpriteSpecifier _icon;
        [DataField("description")]
        private string _description;
        [DataField("requiredPoints")]
        private int _requiredPoints;
        [DataField("requiredTechnologies")]
        private List<string> _requiredTechnologies = new();
        [DataField("unlockedRecipes")]
        private List<string> _unlockedRecipes = new();

        /// <summary>
        ///     The ID of this technology prototype.
        /// </summary>
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string Parent { get; }

        /// <summary>
        ///     The name this technology will have on user interfaces.
        /// </summary>
        [ViewVariables]
        public string Name => _name;

        /// <summary>
        ///     An icon that represent this technology.
        /// </summary>
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     A short description of the technology.
        /// </summary>
        [ViewVariables]
        public string Description => _description;

        /// <summary>
        ///    The required research points to unlock this technology.
        /// </summary>
        [ViewVariables]
        public int RequiredPoints => _requiredPoints;

        /// <summary>
        ///     A list of technology IDs required to unlock this technology.
        /// </summary>
        [ViewVariables]
        public List<string> RequiredTechnologies => _requiredTechnologies;

        /// <summary>
        ///     A list of recipe IDs this technology unlocks.
        /// </summary>
        [ViewVariables]
        public List<string> UnlockedRecipes => _unlockedRecipes;
    }
}

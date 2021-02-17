using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Research
{
    [NetSerializable, Serializable, Prototype("technology")]
    public class TechnologyPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("name")]
        private string _name;
        [YamlField("id")]
        private string _id;
        [YamlField("icon")]
        private SpriteSpecifier _icon;
        [YamlField("description")]
        private string _description;
        [YamlField("requiredPoints")]
        private int _requiredPoints;
        [YamlField("requiredTechnologies")]
        private List<string> _requiredTechnologies = new();
        [YamlField("unlockedRecipes")]
        private List<string> _unlockedRecipes = new();

        /// <summary>
        ///     The ID of this technology prototype.
        /// </summary>
        [ViewVariables]
        public string ID => _id;

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

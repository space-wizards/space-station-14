#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Research
{
    [NetSerializable, Serializable, Prototype("technology")]
    public class TechnologyPrototype : IPrototype
    {
        private string _name = string.Empty;
        private string _id = string.Empty;
        private SpriteSpecifier _icon = SpriteSpecifier.Invalid;
        private string _description = string.Empty;
        private int _requiredPoints;
        private List<string> _requiredTechnologies = new();
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

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(ref _requiredPoints, "requiredPoints", 0);
            serializer.DataField(ref _requiredTechnologies, "requiredTechnologies", new List<string>());
            serializer.DataField(ref _unlockedRecipes, "unlockedRecipes", new List<string>());
        }
    }
}

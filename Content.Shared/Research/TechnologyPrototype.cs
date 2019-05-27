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
    public class TechnologyPrototype : IPrototype, IIndexedPrototype
    {
        private string _name;
        private string _id;
        private SpriteSpecifier _icon;
        private string _description;
        private int _requiredPoints;
        private List<string> _requiredTechnologies;
        private List<string> _unlockedRecipes;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;

        public SpriteSpecifier Icon => _icon;

        [ViewVariables]
        public string Description => _description;

        [ViewVariables]
        public int RequiredPoints => _requiredPoints;

        [ViewVariables]
        public List<string> RequiredTechnologies => _requiredTechnologies;

        [ViewVariables]
        public List<string> UnlockedRecipes => _unlockedRecipes;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(ref _requiredPoints, "requiredpoints", 0);
            serializer.DataField(ref _requiredTechnologies, "requiredtechnologies", new List<string>());
            serializer.DataField(ref _requiredTechnologies, "unlockedrecipes", new List<string>());
        }
    }
}

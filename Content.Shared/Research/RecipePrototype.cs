using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Research
{
    [Prototype("latheRecipe")]
    public class LatheRecipePrototype : IPrototype, IIndexedPrototype
    {
        private string _name;
        private string _id;
        private SpriteSpecifier _icon;
        private string _description;
        private string _result;
        private bool _hacked;
        private string _latheType;

        public string ID => _id;

        /// <summary>
        ///     Name displayed in the lathe GUI.
        /// </summary>
        public string Name => _name;

        /// <summary>
        ///     Short description displayed in the lathe GUI.
        /// </summary>
        public string Description => _description;

        /// <summary>
        ///     Texture path used in the lathe GUI.
        /// </summary>
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     The prototype name of the resulting entity when the recipe is printed.
        /// </summary>
        public string Result => _result;

        /// <summary>
        ///     Whether the lathe should be hacked to unlock this recipe.
        /// </summary>
        public bool Hacked => _hacked;

        /// <summary>
        ///     The type of lathe that'll print this recipe.
        ///     TODO: Replace with an enum before merging, henk!
        /// </summary>
        public string LatheType => _latheType;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            _name = serializer.ReadDataField<string>("name");

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(ref _result, "result", null);
            serializer.DataField(ref _hacked, "hacked", false);
            serializer.DataField(ref _latheType, "lathetype", "default");
        }
    }
}

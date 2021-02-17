using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.BarSign
{
    [Prototype("barSign")]
    public class BarSignPrototype : IPrototype, IIndexedPrototype
    {
        private string _description;
        private string _name;

        [YamlField("id")]
        public string ID { get; private set; }
        [YamlField("icon")]
        public string Icon { get; private set; }

        [YamlField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        [YamlField("description")]
        public string Description
        {
            get => _description;
            private set => _description = Loc.GetString(value);
        }

        [YamlField("renameArea")]
        public bool RenameArea { get; private set; } = true;
        [YamlField("hidden")]
        public bool Hidden { get; private set; }
    }
}

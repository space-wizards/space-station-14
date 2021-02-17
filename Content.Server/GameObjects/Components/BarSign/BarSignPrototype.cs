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

        [DataField("id")]
        public string ID { get; private set; }
        [DataField("icon")]
        public string Icon { get; private set; }

        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        [DataField("description")]
        public string Description
        {
            get => _description;
            private set => _description = Loc.GetString(value);
        }

        [DataField("renameArea")]
        public bool RenameArea { get; private set; } = true;
        [DataField("hidden")]
        public bool Hidden { get; private set; }
    }
}

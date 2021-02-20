using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.BarSign
{
    [Prototype("barSign")]
    public class BarSignPrototype : IPrototype
    {
        public string ID { get; private set; }
        public string Icon { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool RenameArea { get; private set; } = true;
        public bool Hidden { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            Name = Loc.GetString(mapping.GetNode("name").AsString());
            Icon = mapping.GetNode("icon").AsString();

            if (mapping.TryGetNode("hidden", out var node))
            {
                Hidden = node.AsBool();
            }

            if (mapping.TryGetNode("renameArea", out node))
            {
                RenameArea = node.AsBool();
            }

            if (mapping.TryGetNode("description", out node))
            {
                Description = Loc.GetString(node.AsString());
            }
        }
    }
}

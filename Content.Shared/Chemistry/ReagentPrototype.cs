using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Chemistry
{
    [Prototype("reagent")]
    public class ReagentPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Color SubstanceColor { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();
            Name = mapping.GetNode("name").ToString();
            Description = mapping.GetNode("desc").ToString();

            SubstanceColor = mapping.TryGetNode("color", out var colorNode) ? colorNode.AsHexColor(Color.White) : Color.White;
        }
    }
}

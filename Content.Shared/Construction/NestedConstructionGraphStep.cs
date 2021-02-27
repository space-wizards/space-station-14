#nullable enable
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    public class NestedConstructionGraphStep : ConstructionGraphStep
    {
        public List<List<ConstructionGraphStep>> Steps { get; private set; } = new();

        public void LoadFrom(YamlMappingNode mapping)
        {
            if (!mapping.TryGetNode("steps", out YamlSequenceNode? steps)) return;

            foreach (var node in steps)
            {
                var sequence = (YamlSequenceNode) node;
                var list = new List<ConstructionGraphStep>();

                foreach (var innerNode in sequence)
                {
                    var stepNode = (YamlMappingNode) innerNode;
                    var step = ConstructionGraphEdge.LoadStep(stepNode);

                    if(step is NestedConstructionGraphStep)
                        throw new InvalidDataException("Can't have nested construction steps inside nested construction steps!");

                    list.Add(step);
                }

                Steps.Add(list);
            }
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
        }
    }
}

using System.Collections.Generic;
using System.IO;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    public class NestedConstructionGraphStep : ConstructionGraphStep
    {
        public List<List<ConstructionGraphStep>> Steps { get; private set; } = new();

        public void LoadFrom(YamlMappingNode mapping)
        {
            if (!mapping.TryGetNode("steps", out YamlSequenceNode steps)) return;

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

        public override IDeepClone DeepClone()
        {
            var newSteps = new List<List<ConstructionGraphStep>>();
            foreach (var innerlist in Steps)
            {
                var newInnerList = new List<ConstructionGraphStep>();
                foreach (var step in innerlist)
                {
                    newInnerList.Add((ConstructionGraphStep)step.DeepClone());
                }
                newSteps.Add(newInnerList);
            }

            return new NestedConstructionGraphStep {Steps = newSteps};
        }
    }
}

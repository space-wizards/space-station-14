using System;
using System.Collections.Generic;
using System.IO;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Serializable, NetSerializable]
    public class ConstructionGraphEdge : IExposeData
    {
        private List<ConstructionGraphStep> _steps = new List<ConstructionGraphStep>();

        public string Target { get; private set; }
        public List<IEdgeCondition> Conditions { get; private set; }
        public List<IEdgeCompleted> Completed { get; private set; }
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;

        public void ExposeData(ObjectSerializer serializer)
        {
            var moduleManager = IoCManager.Resolve<IModuleManager>();

            serializer.DataField(this, x => x.Target, "to", string.Empty);
            if (!moduleManager.IsServerModule) return;
            serializer.DataField(this, x => x.Conditions, "conditions", new List<IEdgeCondition>());
            serializer.DataField(this, x => x.Completed, "completed", new List<IEdgeCompleted>());
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            ExposeData(serializer);

            if (!mapping.TryGetNode("steps", out YamlSequenceNode stepsMapping)) return;

            foreach (var yamlNode in stepsMapping)
            {
                var stepMapping = (YamlMappingNode) yamlNode;
                var stepSerializer = YamlObjectSerializer.NewReader(stepMapping);

                if (stepMapping.TryGetNode("material", out var _))
                {
                    var material = new MaterialConstructionGraphStep();
                    material.ExposeData(stepSerializer);
                    _steps.Add(material);
                }
                else if (stepMapping.TryGetNode("tool", out var _))
                {
                    var tool = new ToolConstructionGraphStep();
                    tool.ExposeData(stepSerializer);
                    _steps.Add(tool);
                }
                else if (stepMapping.TryGetNode("prototype", out var _))
                {
                    var prototype = new PrototypeConstructionGraphStep();
                    prototype.ExposeData(stepSerializer);
                    _steps.Add(prototype);
                }
                else if (stepMapping.TryGetNode("component", out var _))
                {
                    var component = new ComponentConstructionGraphStep();
                    component.ExposeData(stepSerializer);
                    _steps.Add(component);
                }
                else
                {
                    // TODO CONSTRUCTION NESTED STEPS
                }
            }
        }
    }
}

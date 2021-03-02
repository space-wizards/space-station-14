#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Serializable]
    public class ConstructionGraphEdge : IExposeData
    {
        private List<ConstructionGraphStep> _steps = new();
        private List<IEdgeCondition> _conditions = new();
        private List<IGraphAction> _completed = new();

        [ViewVariables]
        public string Target { get; private set; } = string.Empty;

        [ViewVariables]
        public IReadOnlyList<IEdgeCondition> Conditions => _conditions;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Completed => _completed;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            InternalExposeData(serializer);
        }

        private void InternalExposeData(ObjectSerializer serializer)
        {
            var moduleManager = IoCManager.Resolve<IModuleManager>();

            serializer.DataField(this, x => x.Target, "to", string.Empty);
            if (!moduleManager.IsServerModule) return;
            serializer.DataField(ref _conditions, "conditions", new List<IEdgeCondition>());
            serializer.DataField(ref _completed, "completed", new List<IGraphAction>());
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            InternalExposeData(serializer);

            if (!mapping.TryGetNode("steps", out YamlSequenceNode? stepsMapping)) return;

            foreach (var yamlNode in stepsMapping)
            {
                var stepMapping = (YamlMappingNode) yamlNode;
                _steps.Add(LoadStep(stepMapping));
            }
        }

        public static ConstructionGraphStep LoadStep(YamlMappingNode mapping)
        {
            var stepSerializer = YamlObjectSerializer.NewReader(mapping);

            if (mapping.TryGetNode("material", out _))
            {
                var material = new MaterialConstructionGraphStep();
                material.ExposeData(stepSerializer);
                return material;
            }

            if (mapping.TryGetNode("tool", out _))
            {
                var tool = new ToolConstructionGraphStep();
                tool.ExposeData(stepSerializer);
                return tool;
            }

            if (mapping.TryGetNode("prototype", out _))
            {
                var prototype = new PrototypeConstructionGraphStep();
                prototype.ExposeData(stepSerializer);
                return prototype;
            }

            if (mapping.TryGetNode("component", out _))
            {
                var component = new ComponentConstructionGraphStep();
                component.ExposeData(stepSerializer);
                return component;
            }

            if (mapping.TryGetNode("tag", out _))
            {
                var tags = new TagConstructionGraphStep();
                tags.ExposeData(stepSerializer);
                return tags;
            }

            if (mapping.TryGetNode("allTags", out _) || mapping.TryGetNode("anyTags", out _))
            {
                var tags = new MultipleTagsConstructionGraphStep();
                tags.ExposeData(stepSerializer);
                return tags;
            }

            if(mapping.TryGetNode("steps", out _))
            {
                var nested = new NestedConstructionGraphStep();
                nested.ExposeData(stepSerializer);
                nested.LoadFrom(mapping);
                return nested;
            }

            throw new ArgumentException("Tried to convert invalid YAML node mapping to ConstructionGraphStep!");
        }
    }
}

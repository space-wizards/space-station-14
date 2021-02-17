using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Serializable]
    [YamlDefinition]
    public class ConstructionGraphEdge
    {
        [YamlField("steps")]
        private List<ConstructionGraphStep> _steps = new();
        [YamlField("conditions", serverOnly: true)]
        private List<IEdgeCondition> _conditions;
        [YamlField("completed", serverOnly: true)]
        private List<IGraphAction> _completed;

        [ViewVariables]
        [YamlField("to")]
        public string Target { get; private set; }

        [ViewVariables]
        public IReadOnlyList<IEdgeCondition> Conditions => _conditions;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Completed => _completed;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;
    }
}

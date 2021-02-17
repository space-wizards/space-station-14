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
    [DataDefinition]
    public class ConstructionGraphEdge
    {
        [DataField("steps")]
        private List<ConstructionGraphStep> _steps = new();
        [DataField("conditions", serverOnly: true)]
        private List<IEdgeCondition> _conditions;
        [DataField("completed", serverOnly: true)]
        private List<IGraphAction> _completed;

        [ViewVariables]
        [DataField("to")]
        public string Target { get; private set; }

        [ViewVariables]
        public IReadOnlyList<IEdgeCondition> Conditions => _conditions;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Completed => _completed;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;
    }
}

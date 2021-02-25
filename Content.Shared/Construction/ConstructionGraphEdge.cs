using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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

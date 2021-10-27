using System;
using System.Collections.Generic;
using Content.Shared.Construction.Steps;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Construction
{
    [Serializable]
    [DataDefinition]
    public class ConstructionGraphEdge
    {
        [DataField("steps")]
        private ConstructionGraphStep[] _steps = Array.Empty<ConstructionGraphStep>();

        [DataField("conditions", serverOnly: true)]
        private IGraphCondition[] _conditions = Array.Empty<IGraphCondition>();

        [DataField("completed", serverOnly: true)]
        private IGraphAction[] _completed = Array.Empty<IGraphAction>();

        [ViewVariables]
        [DataField("to", required:true)]
        public string Target { get; } = string.Empty;

        [ViewVariables]
        public IReadOnlyList<IGraphCondition> Conditions => _conditions;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Completed => _completed;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;
    }
}

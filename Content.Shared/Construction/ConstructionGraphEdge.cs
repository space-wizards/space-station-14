using Content.Shared.Construction.Steps;

namespace Content.Shared.Construction
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ConstructionGraphEdge
    {
        [DataField("steps")]
        private ConstructionGraphStep[] _steps = Array.Empty<ConstructionGraphStep>();

        [DataField("conditions", serverOnly: true)]
        private IGraphCondition[] _conditions = Array.Empty<IGraphCondition>();

        [DataField("completed", serverOnly: true)]
        private IGraphAction[] _completed = Array.Empty<IGraphAction>();

        [DataField("to", required:true)]
        public string Target { get; private set; } = string.Empty;

        [ViewVariables]
        public IReadOnlyList<IGraphCondition> Conditions => _conditions;

        [ViewVariables]
        public IReadOnlyList<IGraphAction> Completed => _completed;

        [ViewVariables]
        public IReadOnlyList<ConstructionGraphStep> Steps => _steps;
    }
}

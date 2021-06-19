using System.Collections.Immutable;
using Content.Shared.Body.Surgery.Operation.Effect;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Surgery.Operation.Step.Serializers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation
{
    [Prototype("surgeryOperation")]
    public class SurgeryOperationPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("steps", customTypeSerializer: typeof(OperationStepImmutableListSerializer))]
        public ImmutableList<OperationStep> Steps { get; } = ImmutableList<OperationStep>.Empty;

        [DataField("effect", serverOnly: true)]
        public IOperationEffect? Effect { get; }

        [DataField("hidden")]
        public bool Hidden { get; }
    }
}

using Content.Shared.Body.Surgery.Operation.Step.Conditional;
using Content.Shared.Body.Surgery.Target;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Step
{
    [DataDefinition]
    public class OperationStep
    {
        [DataField("id", required: true)]
        public string Id { get; init; } = string.Empty;

        [DataField("conditional")]
        public IOperationStepConditional? Conditional { get; init; }

        public SurgeryStepPrototype Step(IPrototypeManager prototypeManager)
        {
            return prototypeManager.Index<SurgeryStepPrototype>(Id);
        }

        public bool Necessary(SurgeryTargetComponent target)
        {
            return Conditional?.Necessary(target) ?? true;
        }
    }
}

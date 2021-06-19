using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Target;

namespace Content.Shared.Body.Surgery.Operation.Step.Conditional
{
    public class StumpConditional : IOperationStepConditional
    {
        public bool Necessary(SurgeryTargetComponent target)
        {
            return target.Owner.TryGetComponent(out SharedBodyPartComponent? part) &&
                   part.Body != null &&
                   part.Body.TryGetSlot(part, out var slot) &&
                   slot.HasStump;
        }
    }
}

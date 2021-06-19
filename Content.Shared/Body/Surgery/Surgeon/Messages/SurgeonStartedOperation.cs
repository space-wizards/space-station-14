using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Surgeon.Messages
{
    public class SurgeonStartedOperation : EntityEventArgs
    {
        public SurgeonStartedOperation(SurgeryTargetComponent target, SurgeryOperationPrototype operation)
        {
            Target = target;
            Operation = operation;
        }

        public SurgeryTargetComponent Target { get; }

        public SurgeryOperationPrototype Operation { get; }
    }
}

using Content.Shared.Body.Surgery.Surgeon;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Operation.Messages
{
    public class OperationEnded : EntityEventArgs
    {
        public OperationEnded(SurgeonComponent oldSurgeon, SurgeryOperationPrototype oldOperation)
        {
            OldSurgeon = oldSurgeon;
            OldOperation = oldOperation;
        }

        public SurgeonComponent OldSurgeon { get; }

        public SurgeryOperationPrototype OldOperation { get; }
    }
}

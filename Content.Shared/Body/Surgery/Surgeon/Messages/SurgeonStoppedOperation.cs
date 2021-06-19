using Content.Shared.Body.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Surgeon.Messages
{
    public class SurgeonStoppedOperation : EntityEventArgs
    {
        public SurgeonStoppedOperation(SurgeryTargetComponent oldTarget)
        {
            OldTarget = oldTarget;
        }

        public SurgeryTargetComponent OldTarget { get; }
    }
}

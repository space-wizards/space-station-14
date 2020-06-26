using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent, IInteractHand
    {
        public override string Name => "DisposalEntry";

        private bool TryInsert(IEntity entity)
        {
            if (Parent == null ||
                !entity.TryGetComponent(out DisposableComponent disposable) ||
                disposable.InDisposals)
            {
                return false;
            }

            Contents.Insert(disposable.Owner);
            Parent.Insert(disposable);
            disposable.EnterDisposals(this);
            entity.Transform.GridPosition = Owner.Transform.GridPosition;

            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryInsert(eventArgs.User);
        }
    }
}

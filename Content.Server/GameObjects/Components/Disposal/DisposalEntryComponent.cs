using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalEntryComponent : DisposalTubeComponent, IInteractHand
    {
        public override string Name => "DisposalEntry";

        private bool TryInsert(IEntity entity)
        {
            if (!entity.TryGetComponent(out DisposableComponent disposable) ||
                disposable.InDisposals ||
                !TryInsert(disposable))
            {
                return false;
            }

            entity.Transform.GridPosition = Owner.Transform.GridPosition;
            disposable.EnterDisposals(Parent);

            return true;
        }

        private bool TryRemove(IEntity entity)
        {
            if (!entity.TryGetComponent(out DisposableComponent disposable) ||
                !disposable.InDisposals ||
                !TryRemove(disposable))
            {
                return false;
            }

            entity.Transform.GridPosition = Owner.Transform.GridPosition;
            disposable.ExitDisposals();

            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryInsert(eventArgs.User);
        }
    }
}

using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalEntryComponent : Component, IInteractHand
    {
        public override string Name => "DisposalEntry";

        [ViewVariables] private Container Contents;

        private bool TryInsert(IEntity entity)
        {
            if (!entity.TryGetComponent(out DisposableComponent disposable) ||
                disposable.InDisposals ||
                !Contents.Insert(entity))
            {
                return false;
            }

            disposable.EnterDisposals();
            entity.Transform.GridPosition = Owner.Transform.GridPosition;

            return true;
        }

        private bool TryRemove(IEntity entity)
        {
            if (!entity.TryGetComponent(out DisposableComponent disposable) ||
                !disposable.InDisposals ||
                !Contents.Remove(entity))
            {
                return false;
            }

            entity.Transform.GridPosition = Owner.Transform.GridPosition;

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            Contents = ContainerManagerComponent.Ensure<Container>(nameof(DisposalEntryComponent), Owner);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryInsert(eventArgs.User);
        }
    }
}

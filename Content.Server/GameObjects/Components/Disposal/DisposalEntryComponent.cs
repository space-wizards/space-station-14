using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalEntry";

        private bool CanInsert(IEntity entity)
        {
            return entity.HasComponent<ItemComponent>() ||
                   entity.HasComponent<SpriteComponent>();
        }

        public bool TryInsert(IEntity entity)
        {
            if (!CanInsert(entity) || Parent == null)
            {
                return false;
            }

            var disposable = entity.EnsureComponent<DisposableComponent>();

            Contents.Insert(disposable.Owner);
            Parent.Insert(disposable);
            disposable.EnterDisposals(this);
            entity.Transform.GridPosition = Owner.Transform.GridPosition;

            return true;
        }
    }
}

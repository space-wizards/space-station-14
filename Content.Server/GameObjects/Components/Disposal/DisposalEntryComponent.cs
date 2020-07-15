using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalEntry";

        private bool CanInsert(IEntity entity)
        {
            return entity.HasComponent<DisposableComponent>();
        }

        public bool TryInsert(IEntity entity)
        {
            if (!CanInsert(entity) || !Contents.Insert(entity))
            {
                return false;
            }

            var disposable = entity.EnsureComponent<DisposableComponent>();
            disposable.EnterTube(this);

            return true;
        }

        public override Direction[] ConnectableDirections()
        {
            return new[] {Owner.Transform.LocalRotation.GetDir()};
        }

        public override Direction NextDirection(DisposableComponent disposable)
        {
            return ConnectableDirections()[0];
        }
    }
}

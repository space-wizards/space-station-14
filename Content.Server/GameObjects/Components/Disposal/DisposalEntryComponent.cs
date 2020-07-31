using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        private const string HolderPrototypeId = "DisposalHolder";

        public override string Name => "DisposalEntry";

        public bool TryInsert(IReadOnlyCollection<IEntity> entities)
        {
            var holder = Owner.EntityManager.SpawnEntity(HolderPrototypeId, Owner.Transform.MapPosition);
            var holderComponent = holder.GetComponent<DisposalHolderComponent>();

            foreach (var entity in entities)
            {
                holderComponent.TryInsert(entity);
            }

            return TryInsert(holderComponent);
        }

        public bool TryInsert(DisposalHolderComponent holder)
        {
            if (!Contents.Insert(holder.Owner))
            {
                return false;
            }

            holder.EnterTube(this);

            return true;
        }

        protected override Direction[] ConnectableDirections()
        {
            return new[] {Owner.Transform.LocalRotation.GetDir()};
        }

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            return ConnectableDirections()[0];
        }
    }
}

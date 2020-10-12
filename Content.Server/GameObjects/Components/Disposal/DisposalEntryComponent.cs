using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

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

        private bool TryInsert(DisposalHolderComponent holder)
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

        /// <summary>
        ///     Ejects contents when they come from the same direction the entry is facing.
        /// </summary>
        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            if (holder.PreviousTube != null && DirectionTo(holder.PreviousTube) == ConnectableDirections()[0])
            {
                var invalidDirections = new Direction[] { ConnectableDirections()[0], Direction.Invalid };
                var directions = Enum.GetValues(typeof(Direction))
                    .Cast<Direction>().Except(invalidDirections).ToList();
                return _random.Pick<Direction>(directions);
            }

            return ConnectableDirections()[0];
        }
    }
}

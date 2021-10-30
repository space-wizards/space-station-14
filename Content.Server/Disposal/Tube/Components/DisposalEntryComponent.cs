using System;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Unit.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private const string HolderPrototypeId = "DisposalHolder";

        public override string Name => "DisposalEntry";

        public bool TryInsert(DisposalUnitComponent from)
        {
            var holder = Owner.EntityManager.SpawnEntity(HolderPrototypeId, Owner.Transform.MapPosition);
            var holderComponent = holder.GetComponent<DisposalHolderComponent>();

            foreach (var entity in from.ContainedEntities.ToArray())
            {
                holderComponent.TryInsert(entity);
            }

            EntitySystem.Get<AtmosphereSystem>().Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

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

        /// <summary>
        ///     Ejects contents when they come from the same direction the entry is facing.
        /// </summary>
        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            if (holder.PreviousDirectionFrom != Direction.Invalid && holder.PreviousDirectionFrom == ConnectableDirections()[0])
            {
                var invalidDirections = new[] { ConnectableDirections()[0], Direction.Invalid };
                var directions = Enum.GetValues(typeof(Direction))
                    .Cast<Direction>().Except(invalidDirections).ToList();
                return _random.Pick(directions);
            }

            return ConnectableDirections()[0];
        }
    }
}

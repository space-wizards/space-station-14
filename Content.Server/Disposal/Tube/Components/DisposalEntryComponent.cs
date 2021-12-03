using System;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
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
        private const string HolderPrototypeId = "DisposalHolder";

        public override string Name => "DisposalEntry";

        public bool TryInsert(DisposalUnitComponent from)
        {
            var holder = IoCManager.Resolve<IEntityManager>().SpawnEntity(HolderPrototypeId, Owner.Transform.MapPosition);
            var holderComponent = IoCManager.Resolve<IEntityManager>().GetComponent<DisposalHolderComponent>(holder.Uid);

            foreach (var entity in from.ContainedEntities.ToArray())
            {
                holderComponent.TryInsert(entity);
            }

            EntitySystem.Get<AtmosphereSystem>().Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

            return EntitySystem.Get<DisposableSystem>().EnterTube(holderComponent.OwnerUid, OwnerUid, holderComponent, null, this);
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
            if (holder.PreviousDirectionFrom != Direction.Invalid)
            {
                return Direction.Invalid;
            }

            return ConnectableDirections()[0];
        }
    }
}

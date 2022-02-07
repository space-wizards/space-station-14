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
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const string HolderPrototypeId = "DisposalHolder";

        public bool TryInsert(DisposalUnitComponent from)
        {
            var holder = _entMan.SpawnEntity(HolderPrototypeId, _entMan.GetComponent<TransformComponent>(Owner).MapPosition);
            var holderComponent = _entMan.GetComponent<DisposalHolderComponent>(holder);

            foreach (var entity in from.Container.ContainedEntities.ToArray())
            {
                holderComponent.TryInsert(entity);
            }

            EntitySystem.Get<AtmosphereSystem>().Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

            return EntitySystem.Get<DisposableSystem>().EnterTube((holderComponent).Owner, Owner, holderComponent, null, this);
        }

        protected override Direction[] ConnectableDirections()
        {
            return new[] {_entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetDir()};
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

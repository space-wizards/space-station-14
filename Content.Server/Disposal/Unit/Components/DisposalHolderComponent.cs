using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Items;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Disposal.Unit.Components
{
    // TODO: Add gas
    [RegisterComponent]
    public class DisposalHolderComponent : Component, IGasMixtureHolder
    {
        public override string Name => "DisposalHolder";

        public Container Container = null!;

        /// <summary>
        ///     The total amount of time that it will take for this entity to
        ///     be pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float StartingTime { get; set; }

        /// <summary>
        ///     Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float TimeLeft { get; set; }

        [ViewVariables]
        public IDisposalTubeComponent? PreviousTube { get; set; }

        [ViewVariables]
        public Direction PreviousDirection { get; set; } = Direction.Invalid;

        [ViewVariables]
        public Direction PreviousDirectionFrom => (PreviousDirection == Direction.Invalid) ? Direction.Invalid : PreviousDirection.GetOpposite();

        [ViewVariables]
        public IDisposalTubeComponent? CurrentTube { get; set; }

        // CurrentDirection is not null when CurrentTube isn't null.
        [ViewVariables]
        public Direction CurrentDirection { get; set; } = Direction.Invalid;

        /// <summary>
        ///     A list of tags attached to the content, used for sorting
        /// </summary>
        [ViewVariables]
        public HashSet<string> Tags { get; set; } = new();

        [ViewVariables]
        [DataField("air")]
        public GasMixture Air { get; set; } = new GasMixture(Atmospherics.CellVolume);

        protected override void Initialize()
        {
            base.Initialize();

            Container = ContainerHelpers.EnsureContainer<Container>(Owner, nameof(DisposalHolderComponent));
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EntitySystem.Get<DisposableSystem>().ExitDisposals(OwnerUid);
        }

        private bool CanInsert(IEntity entity)
        {
            if (!Container.CanInsert(entity))
            {
                return false;
            }

            return entity.HasComponent<ItemComponent>() ||
                   entity.HasComponent<SharedBodyComponent>();
        }

        public bool TryInsert(IEntity entity)
        {
            if (!CanInsert(entity) || !Container.Insert(entity))
            {
                return false;
            }

            if (entity.TryGetComponent(out IPhysBody? physics))
            {
                physics.CanCollide = false;
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Tube;
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

        private Container _contents = null!;

        /// <summary>
        ///     The total amount of time that it will take for this entity to
        ///     be pushed to the next tube
        /// </summary>
        [ViewVariables]
        private float StartingTime { get; set; }

        /// <summary>
        ///     Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        private float TimeLeft { get; set; }

        [ViewVariables]
        public IDisposalTubeComponent? PreviousTube { get; set; }

        [ViewVariables]
        public Direction PreviousDirection { get; private set; } = Direction.Invalid;

        [ViewVariables]
        public Direction PreviousDirectionFrom => (PreviousDirection == Direction.Invalid) ? Direction.Invalid : PreviousDirection.GetOpposite();

        [ViewVariables]
        public IDisposalTubeComponent? CurrentTube { get; private set; }

        // CurrentDirection is not null when CurrentTube isn't null.
        [ViewVariables]
        public Direction CurrentDirection { get; private set; } = Direction.Invalid;

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

            _contents = ContainerHelpers.EnsureContainer<Container>(Owner, nameof(DisposalHolderComponent));
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            ExitDisposals();
        }

        private bool CanInsert(IEntity entity)
        {
            if (!_contents.CanInsert(entity))
            {
                return false;
            }

            return entity.HasComponent<ItemComponent>() ||
                   entity.HasComponent<SharedBodyComponent>();
        }

        public bool TryInsert(IEntity entity)
        {
            if (!CanInsert(entity) || !_contents.Insert(entity))
            {
                return false;
            }

            if (entity.TryGetComponent(out IPhysBody? physics))
            {
                physics.CanCollide = false;
            }

            return true;
        }

        public void EnterTube(IDisposalTubeComponent tube)
        {
            if (CurrentTube != null)
            {
                PreviousTube = CurrentTube;
                PreviousDirection = CurrentDirection;
            }

            Owner.Transform.Coordinates = tube.Owner.Transform.Coordinates;
            CurrentTube = tube;
            CurrentDirection = tube.NextDirection(this);
            StartingTime = 0.1f;
            TimeLeft = 0.1f;
        }

        public void ExitDisposals()
        {
            if (Deleted)
                return;

            PreviousTube = null;
            PreviousDirection = Direction.Invalid;
            CurrentTube = null;
            CurrentDirection = Direction.Invalid;
            StartingTime = 0;
            TimeLeft = 0;

            foreach (var entity in _contents.ContainedEntities.ToArray())
            {
                if (entity.TryGetComponent(out IPhysBody? physics))
                {
                    physics.CanCollide = true;
                }

                _contents.ForceRemove(entity);

                if (entity.Transform.Parent == Owner.Transform)
                {
                    entity.Transform.AttachParentToContainerOrGrid();
                }
            }

            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            if (atmosphereSystem.GetTileMixture(Owner.Transform.Coordinates, true) is {} environment)
            {
                atmosphereSystem.Merge(environment, Air);
                Air.Clear();
            }

            Owner.Delete();
        }

        public void Update(float frameTime)
        {
            while (frameTime > 0)
            {
                var time = frameTime;
                if (time > TimeLeft)
                {
                    time = TimeLeft;
                }

                TimeLeft -= time;
                frameTime -= time;

                if (CurrentTube == null || CurrentTube.Deleted)
                {
                    ExitDisposals();
                    break;
                }

                if (TimeLeft > 0)
                {
                    var progress = 1 - TimeLeft / StartingTime;
                    var origin = CurrentTube.Owner.Transform.Coordinates;
                    var destination = CurrentDirection.ToVec();
                    var newPosition = destination * progress;

                    Owner.Transform.Coordinates = origin.Offset(newPosition);

                    continue;
                }

                var nextTube = EntitySystem.Get<DisposalTubeSystem>().NextTubeFor(CurrentTube.Owner.Uid, CurrentDirection);
                if (nextTube == null || nextTube.Deleted || !CurrentTube.TransferTo(this, nextTube))
                {
                    CurrentTube.Remove(this);
                    break;
                }
            }
        }
    }
}

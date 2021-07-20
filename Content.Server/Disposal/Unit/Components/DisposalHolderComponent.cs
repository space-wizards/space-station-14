using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Interfaces;
using Content.Server.Items;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
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
        public IDisposalTubeComponent? CurrentTube { get; private set; }

        [ViewVariables]
        public IDisposalTubeComponent? NextTube { get; set; }

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
            }

            Owner.Transform.Coordinates = tube.Owner.Transform.Coordinates;
            CurrentTube = tube;
            NextTube = tube.NextTube(this);
            StartingTime = 0.1f;
            TimeLeft = 0.1f;
        }

        public void ExitDisposals()
        {
            if (Deleted)
                return;

            PreviousTube = null;
            CurrentTube = null;
            NextTube = null;
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

                if (CurrentTube == null)
                {
                    ExitDisposals();
                    break;
                }

                if (TimeLeft > 0)
                {
                    var progress = 1 - TimeLeft / StartingTime;
                    var origin = CurrentTube.Owner.Transform.WorldPosition;
                    var destination = CurrentTube.NextDirection(this).ToVec();
                    var newPosition = destination * progress;

                    Owner.Transform.WorldPosition = origin + newPosition;

                    continue;
                }

                if (NextTube == null || !CurrentTube.TransferTo(this, NextTube))
                {
                    CurrentTube.Remove(this);
                    break;
                }
            }
        }
    }
}

#nullable enable
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : SharedDisposableComponent, IDragDrop
    {
        private bool _inDisposals;

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

        /// <summary>
        ///     Whether or not this entity is currently inside a disposal tube
        /// </summary>
        [ViewVariables]
        public override bool InTube
        {
            get => _inDisposals;
            protected set
            {
                _inDisposals = value;
                Dirty();
            }
        }

        [ViewVariables]
        public IDisposalTubeComponent? PreviousTube { get; set; }

        [ViewVariables]
        public IDisposalTubeComponent? CurrentTube { get; private set; }

        [ViewVariables]
        public IDisposalTubeComponent? NextTube { get; set; }

        public void EnterTube(IDisposalTubeComponent tube)
        {
            InTube = true;

            if (CurrentTube != null)
            {
                PreviousTube = CurrentTube;
            }

            Owner.Transform.GridPosition = tube.Owner.Transform.GridPosition;
            CurrentTube = tube;
            NextTube = tube.NextTube(this);
            StartingTime = 0.1f;
            TimeLeft = 0.1f;
        }

        public void ExitDisposals()
        {
            InTube = false;
            PreviousTube = null;
            CurrentTube = null;
            NextTube = null;
            StartingTime = 0;
            TimeLeft = 0;

            if (!Owner.Transform.IsMapTransform)
            {
                Owner.Transform.AttachToGridOrMap();
            }
        }

        public void Update(float frameTime)
        {
            if (!InTube)
            {
                return;
            }

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

        public override void OnRemove()
        {
            base.OnRemove();
            ExitDisposals();
        }

        public override ComponentState GetComponentState()
        {
            return new DisposableComponentState(InTube);
        }

        bool IDragDrop.CanDragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<DisposalUnitComponent>();
        }

        bool IDragDrop.DragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.Target.TryGetComponent(out DisposalUnitComponent unit) &&
                   unit.TryInsert(Owner);
        }
    }
}

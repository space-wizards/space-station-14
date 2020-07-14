#nullable enable
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : SharedDisposableComponent, IActionBlocker, IDragDrop
    {
        private bool _inDisposals;

        [ViewVariables]
        public IDisposalTubeComponent? PreviousTube { get; set; }

        [ViewVariables]
        private IDisposalTubeComponent? CurrentTube { get; set; }

        [ViewVariables]
        public IDisposalTubeComponent? NextTube { get; set; }

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

        public void EnterTube(IDisposalTubeComponent tube)
        {
            _inDisposals = true;

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
            _inDisposals = false;
            PreviousTube = null;
            CurrentTube = null;
            NextTube = null;
            StartingTime = 0;
            TimeLeft = 0;
            Owner.Transform.DetachParent();
        }

        public void Update(float frameTime)
        {
            if (!_inDisposals)
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

        bool IActionBlocker.CanInteract()
        {
            return !_inDisposals;
        }

        bool IActionBlocker.CanThrow()
        {
            return !_inDisposals;
        }

        bool IActionBlocker.CanDrop()
        {
            return !_inDisposals;
        }

        bool IActionBlocker.CanPickup()
        {
            return !_inDisposals;
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

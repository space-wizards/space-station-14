#nullable enable
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class InDisposalsComponent : Component, IActionBlocker
    {
        public override string Name => "InDisposals";

#pragma warning disable 649
        [Dependency] private readonly IComponentManager _componentManager = default!;
#pragma warning restore 649

        [ViewVariables]
        public IDisposalTubeComponent? PreviousTube { get; private set; }

        [ViewVariables]
        private IDisposalTubeComponent? CurrentTube { get; set; }

        [ViewVariables]
        private IDisposalTubeComponent? NextTube { get; set; }

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
            PreviousTube = null;
            CurrentTube = null;
            NextTube = null;
            StartingTime = 0;
            TimeLeft = 0;
            Owner.Transform.DetachParent();

            _componentManager.RemoveComponent(Owner.Uid, this);
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

        public override void OnRemove()
        {
            base.OnRemove();
            ExitDisposals();
        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }
    }
}

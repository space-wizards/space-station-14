using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : Component, IActionBlocker
    {
        public override string Name => "Disposable";

        [ViewVariables]
        public bool InDisposals { get; private set; }

        [ViewVariables, CanBeNull]
        private IDisposalTubeComponent DisposalTube { get; set; }

        /// <summary>
        /// Time left until the entity is pushed to the next tube
        /// </summary>
        [ViewVariables]
        public float TimeLeft { get; set; }

        public void EnterDisposals(IDisposalTubeComponent tube)
        {
            InDisposals = true;
            DisposalTube = tube;
            TimeLeft = tube.Parent.Speed;
            Owner.Transform.WorldPosition = tube.Owner.Transform.WorldPosition;
        }

        public void ExitDisposals()
        {
            InDisposals = false;
            DisposalTube?.Parent?.Remove(this);
            DisposalTube = null;
            TimeLeft = 0;
        }

        public void Update(float frameTime)
        {
            DisposalTube?.Update(frameTime, Owner);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            ExitDisposals();
        }

        bool IActionBlocker.CanMove()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanInteract()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanUse()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanThrow()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanDrop()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanPickup()
        {
            return !InDisposals;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return !InDisposals;
        }
    }
}

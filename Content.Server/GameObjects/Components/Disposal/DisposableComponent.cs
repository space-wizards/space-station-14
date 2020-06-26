using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : Component, IActionBlocker
    {
        public override string Name => "Disposable";

        public bool InDisposals { get; private set; }

        [CanBeNull]
        private DisposalNet DisposalNet { get; set; }

        public void EnterDisposals(DisposalNet net)
        {
            InDisposals = true;
            DisposalNet = net;
        }

        public void ExitDisposals()
        {
            InDisposals = false;
            DisposalNet?.Remove(this);
            DisposalNet = null;
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

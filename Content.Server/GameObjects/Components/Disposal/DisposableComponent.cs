using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposableComponent : Component, IActionBlocker
    {
        public override string Name => "Disposable";

        public bool InDisposals { get; private set; }

        public void EnterDisposals()
        {
            InDisposals = true;
        }

        public void ExitDisposals()
        {
            InDisposals = false;
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

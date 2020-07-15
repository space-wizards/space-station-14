using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposableComponent : Component, IActionBlocker
    {
        public override string Name => "Disposable";

        public override uint? NetID => ContentNetIDs.DISPOSABLE;

        protected abstract bool InDisposals { get; set; }

        bool IActionBlocker.CanInteract()
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
    }

    public sealed class DisposableComponentState : ComponentState
    {
        public readonly bool InDisposals;

        public DisposableComponentState(bool inDisposals) : base(ContentNetIDs.DISPOSABLE)
        {
            InDisposals = inDisposals;
        }
    }
}

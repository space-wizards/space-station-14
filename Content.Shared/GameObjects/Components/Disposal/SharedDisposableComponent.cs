using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposableComponent : Component, IActionBlocker
    {
        public override string Name => "Disposable";

        public override uint? NetID => ContentNetIDs.DISPOSABLE;

        public abstract bool InTube { get; protected set; }

        bool IActionBlocker.CanInteract()
        {
            return !InTube;
        }

        bool IActionBlocker.CanThrow()
        {
            return !InTube;
        }

        bool IActionBlocker.CanDrop()
        {
            return !InTube;
        }

        bool IActionBlocker.CanPickup()
        {
            return !InTube;
        }
    }

    [Serializable, NetSerializable]
    public sealed class DisposableComponentState : ComponentState
    {
        public readonly bool InDisposals;

        public DisposableComponentState(bool inDisposals) : base(ContentNetIDs.DISPOSABLE)
        {
            InDisposals = inDisposals;
        }
    }
}

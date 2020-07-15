using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    public abstract class SharedDisposalUnitComponent : Component
    {
        public override string Name => "DisposalUnit";
    }

    [Serializable, NetSerializable]
    public enum DisposalUnitVisuals
    {
        VisualState,
    }

    [Serializable, NetSerializable]
    public enum DisposalUnitVisualState
    {
        UnAnchored,
        Anchored,
        Flushing
    }
}

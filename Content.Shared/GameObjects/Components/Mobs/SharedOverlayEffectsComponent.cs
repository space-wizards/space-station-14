using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Full screen overlays; Blindness, death, flash, alcohol etc.
    /// </summary>
    public abstract class SharedOverlayEffectsComponent : Component
    {
        public override string Name => "OverlayEffectsUI";
        public sealed override uint? NetID => ContentNetIDs.OVERLAYEFFECTS;
        public sealed override Type StateType => typeof(OverlayEffectComponentState);
    }

    public enum ScreenEffects
    {
        None,
        CircleMask,
        GradientCircleMask,
    }

    [Serializable, NetSerializable]
    public class OverlayEffectComponentState : ComponentState
    {
        public ScreenEffects ScreenEffect;

        public OverlayEffectComponentState(ScreenEffects screenEffect) : base(ContentNetIDs.OVERLAYEFFECTS)
        {
            ScreenEffect = screenEffect;
        }
    }
}

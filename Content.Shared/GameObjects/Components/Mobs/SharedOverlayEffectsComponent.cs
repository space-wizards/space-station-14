using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Full screen overlays; Blindness, death, flash, alcohol etc.
    /// </summary>
    public class SharedOverlayEffectsComponent : Component
    {
        public override string Name => "OverlayEffectsUI";
        public sealed override uint? NetID => ContentNetIDs.OVERLAYEFFECTS;
    }

    public enum ScreenEffects
    {
        None,
        CircleMask,
        GradientCircleMask,
    }

    [Serializable, NetSerializable]
    public class OverlayEffectMessage : ComponentMessage
    {
        public ScreenEffects Effect;
        public OverlayEffectMessage(ScreenEffects effect)
        {
            Effect = effect;
            Directed = true;
        }
    }
}

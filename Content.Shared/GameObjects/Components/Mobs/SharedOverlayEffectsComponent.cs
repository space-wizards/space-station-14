using System;
using System.Collections.Generic;
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
    }

    [Serializable, NetSerializable]
    public class OverlayEffectComponentState : ComponentState
    {
        public List<string> ScreenEffects;

        public OverlayEffectComponentState(List<string> screenEffects) : base(ContentNetIDs.OVERLAYEFFECTS)
        {
            ScreenEffects = screenEffects;
        }
    }
}

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects
{
    /// <summary>
    /// Sends updates to the standard species health hud with the sprite to change the hud to
    /// </summary>
    [Serializable, NetSerializable]
    public class HudStateChange : ComponentMessage
    {
        public string StateSprite;
        public ScreenEffects effect;

        public HudStateChange()
        {
            Directed = true;
        }
    }

    public enum ScreenEffects
    {
        None,
        CircleMask,
        GradientCircleMask,
    }
}

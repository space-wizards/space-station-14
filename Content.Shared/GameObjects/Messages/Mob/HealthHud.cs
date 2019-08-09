using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using System;

namespace Content.Shared.GameObjects
{
    /// <summary>
    /// Sends updates to the standard species health hud with the sprite to change the hud to
    /// </summary>
    [Serializable, NetSerializable]
    public class HudStateChange : ComponentMessage
    {
        public List<LimbRender> StateSprites;
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

    [Serializable, NetSerializable]
    public class LimbRender
    {
        public string Name;
        public Color? Color;

        public LimbRender(string name, Color? color = null)
        {
            Name = name;
            Color = color;
        }
    }
}

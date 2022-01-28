using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Sprite.Components
{
    [RegisterComponent, ComponentProtoName("RandomSpriteColor")]
    public class RandomSpriteColorComponent : Component
    {
        // This should handle random states + colors for layers.
        // Saame with RandomSpriteState
        [DataField("selected")] public string? SelectedColor;
        [DataField("state")] public string BaseState = "error";

        [DataField("colors")] public readonly Dictionary<string, Color> Colors = new();
    }
}

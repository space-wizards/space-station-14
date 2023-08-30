using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeAppearanceComponent : Component
{
    [DataField("sprite")]
    public SpriteSpecifier.Rsi Sprite = new(new("Structures/Piping/Atmospherics/pipe.rsi"), "pipeConnector");
}

using Robust.Shared.Animations;

namespace Content.Shared.Implants;

[RegisterComponent]
public sealed partial class ItemLayersColorComponent : Component
{
    [DataField("color")]
    private Color _color = Color.Green;

    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color Color
    {
        get => _color;
        set => _color = value;
    }
}

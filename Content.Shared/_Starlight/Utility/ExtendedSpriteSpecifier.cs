using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Starlight.Utility;

[Serializable, NetSerializable]
[DataDefinition]
public partial class ExtendedSpriteSpecifier
{
    
    /// <summary>
    /// Basic SpriteSpecifier
    /// </summary>
    [DataField("sprite")]
    public SpriteSpecifier Sprite { get; internal set; }
    
    /// <summary>
    /// Sprite Color(Additional)
    /// </summary>
    [DataField("color")]
    public Color Color { get; internal set; }

    public ExtendedSpriteSpecifier(SpriteSpecifier sprite, Color? color = null)
    {
        Sprite = sprite;
        Color = color ?? Color.White;
    }
}
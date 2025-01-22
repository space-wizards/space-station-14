using System.Numerics;
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
    public Color SpriteColor = Color.White;
    
    [DataField("scale")]
    public Vector2 SpriteScale = new(1, 1);
    
    [DataField("noRot")]
    public bool SpriteRotation = true;

    public ExtendedSpriteSpecifier(SpriteSpecifier sprite, Color? color = null, Vector2? scale = null, bool? rotation = null)
    {
        Sprite = sprite;
        SpriteColor = color ?? Color.White;
        SpriteScale = scale ?? new(1, 1);
        SpriteRotation = rotation ?? true;
    }
}
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

[Serializable, NetSerializable]
public enum MarkingColoringType : byte
{
    SimpleColor,
    SkinColor,
    HairColor,
    FacialHairColor,
    AnyHairColor,
    Tattoo,
    EyeColor
}


[DataDefinition]
[Serializable, NetSerializable]
public sealed class MarkingColors
{
    /// <summary>
    /// Coloring properties that will be used on any unspecified layer
    /// </summary>
    [DataField("default", true)]
    public ColoringProperties Default { get; } = new();

    /// <summary>
    /// Layers with their own coloring type and properties
    /// </summary>
    [DataField("layers", true)]
    public Dictionary<string, ColoringProperties>? Layers;
}

/// <summary>
/// Properties for coloring. 
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed class ColoringProperties
{
    /// <summary>
    /// Coloring type that will be used on that layer
    /// </summary>
    [DataField("type", true, required: true)]
    public MarkingColoringType Type { get; } = MarkingColoringType.SkinColor;

    /// <summary>
    /// Color that will be used if coloring type can't be performed or used "Color" type
    /// </summary>
    [DataField("color", true)]
    public Color Color { get; } = new (1f, 1f, 1f, 1f);

    /// <summary>
    /// Makes output color negative if true (in e.x. Different Eyes)
    /// </summary>
    [DataField("negative", true)]
    public bool Negative { get; } = false;

    /// <summary>
    /// Makes marking unchangeable in preferences
    /// </summary>
    [DataField("forced", true)]
    public bool Forced { get; } = false;
}


public static class MarkingColoring
{
    public static List<Color> GetMarkingLayerColors
    (
        MarkingPrototype prototype,
        Color? skinColor,
        Color? eyeColor,
        Color? hairColor,
        Color? facialHairColor
    )
    {
        List<Color> colors = new List<Color>();

        // Coloring from default properties
        Color default_color = MarkingColoring.GetMarkingColor(
            prototype.Coloring.Default,
            skinColor,
            eyeColor,
            hairColor,
            facialHairColor
        );

        if (prototype.Coloring.Layers == null)
        {
            // If layers is not specified, then every layer must be default
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                colors.Add(default_color);
            }
            return colors;
        }
        else
        {
            // If some layers are specified.
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                // Getting layer name
                string name;
                switch (prototype.Sprites[i])
                {
                    case SpriteSpecifier.Rsi rsi:
                        name = rsi.RsiState;
                        break;
                    case SpriteSpecifier.Texture texture:
                        name = texture.TexturePath.Filename;
                        break;
                    default:
                        colors.Add(default_color);
                        continue;
                }
            
                // All specified layers must be colored separately, all unspecified must depend on default coloring
                if (prototype.Coloring.Layers.TryGetValue(name, out ColoringProperties? properties))
                {
                    Color marking_color = MarkingColoring.GetMarkingColor(
                        properties,
                        skinColor,
                        eyeColor,
                        hairColor,
                        facialHairColor
                    );
                    colors.Add(marking_color);
                }
                else
                {
                    colors.Add(default_color);
                }
            }
            return colors;
        }
    }


    public static Color GetMarkingColor
    (
        ColoringProperties properties,
        Color? skinColor,
        Color? eyeColor,
        Color? hairColor,
        Color? facialHairColor
    )
    {
        Color out_color;
        switch (properties.Type)
        {
            case MarkingColoringType.SimpleColor:
                out_color = SimpleColor(properties.Color);
                break;
            case MarkingColoringType.SkinColor:
                out_color = SkinColor(skinColor);
                break;
            case MarkingColoringType.HairColor:
                out_color = HairColor(skinColor, hairColor);
                break;
            case MarkingColoringType.FacialHairColor:
                out_color = HairColor(skinColor, facialHairColor);
                break;
            case MarkingColoringType.AnyHairColor:
                out_color = AnyHairColor(skinColor, hairColor, facialHairColor);
                break;
            case MarkingColoringType.EyeColor:
                out_color = EyeColor(eyeColor);
                break;
            case MarkingColoringType.Tattoo:
                out_color = Tattoo(skinColor);
                break;
            default:
                out_color = properties.Color;
                break;
        }

        // Negative color
        if (properties.Negative)
        {
            out_color.R = 1f-out_color.R;
            out_color.G = 1f-out_color.G;
            out_color.B = 1f-out_color.B;
        }

        return out_color;
    }

    public static Color AnyHairColor(Color? skinColor, Color? hairColor, Color? facialHairColor)
    {
        return hairColor ?? facialHairColor ?? skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color HairColor(Color? skinColor, Color? hairColor)
    {
        return hairColor ?? skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FacialHairColor(Color? skinColor, Color? facialHairColor)
    {
        return facialHairColor ?? skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color SkinColor(Color? skinColor)
    {
        return skinColor ?? new (1f, 1f, 1f, 1f);
    }
    
    public static Color Tattoo(Color? skinColor)
    {
        var newColor = Color.ToHsv(skinColor ?? new (1f, 1f, 1f, 1f));
        newColor.Y = .15f;
        newColor.Z = .20f;

        return Color.FromHsv(newColor);
    }

    public static Color EyeColor(Color? eyeColor)
    {
        return eyeColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color SimpleColor(Color? color)
    {
        return color ?? new Color(1f, 1f, 1f, 1f);
    }
}

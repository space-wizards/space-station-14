using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

[Serializable, NetSerializable]
public enum MarkingColoringType : byte
{
    /// <summary>
    ///     Color applied from "color" property
    /// </summary>
    SimpleColor,

    /// <summary>
    ///     Color applied from humanoid skin
    /// </summary>
    SkinColor,

    /// <summary>
    ///     Color applied from humanoid hair color
    /// </summary>
    HairColor,
    
    /// <summary>
    ///     Color applied from humanoid facial hair color
    /// </summary>
    FacialHairColor,

    /// <summary>
    ///     Color applied from humanoid hair or facial hair color
    /// </summary>
    AnyHairColor,

    /// <summary>
    ///     Color applied like it's tattoo
    /// </summary>
    Tattoo,

    /// <summary>
    ///     Color applied from humanoid eye color
    /// </summary>
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
    ///     Layers with their own coloring type and properties
    /// </summary>
    [DataField("layers", true)]
    public Dictionary<string, ColoringProperties>? Layers;
}

/// <summary>
///     Properties for coloring. 
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed class ColoringProperties
{
    /// <summary>
    ///     Coloring type that will be used on that layer
    /// </summary>
    [DataField("type", true, required: true)]
    public MarkingColoringType Type { get; } = MarkingColoringType.SkinColor;

    /// <summary>
    ///     Color that will be used if coloring type can't be performed or used "SimpleColor" type
    /// </summary>
    [DataField("color", true)]
    public Color Color { get; } = Color.White;

    /// <summary>
    ///     Makes output color negative if true (in e.x. Different Eyes)
    /// </summary>
    [DataField("negative", true)]
    public bool Negative { get; } = false;
}


public static class MarkingColoring
{
    /// <summary>
    ///     Returns list of colors for marking layers
    /// </summary>
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
        Color outColor = properties.Type switch {
            MarkingColoringType.SimpleColor => SimpleColor(properties.Color),
            MarkingColoringType.SkinColor => SkinColor(skinColor),
            MarkingColoringType.HairColor => HairColor(skinColor, hairColor),
            MarkingColoringType.FacialHairColor => HairColor(skinColor, facialHairColor),
            MarkingColoringType.AnyHairColor => AnyHairColor(skinColor, hairColor, facialHairColor),
            MarkingColoringType.EyeColor => EyeColor(skinColor),
            MarkingColoringType.Tattoo => Tattoo(skinColor),
            _ => properties.Color
        };

        // Negative color
        if (properties.Negative)
        {
            outColor.R = 1f-outColor.R;
            outColor.G = 1f-outColor.G;
            outColor.B = 1f-outColor.B;
        }

        return outColor;
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

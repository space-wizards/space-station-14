using Robust.Shared.Utility;
using Robust.Shared.Serialization;

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
    ///     Color applied from skin, but much darker.
    /// </summary>
    Tattoo,

    /// <summary>
    ///     Color applied from humanoid eye color
    /// </summary>
    EyeColor
}


/// <summary>
///     Default colors for marking 
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed class MarkingColors
{
    /// <summary>
    /// Coloring properties that will be used on any unspecified layer
    /// </summary>
    [DataField("default", true)]
    public LayerColoring Default = new SkinColoring();

    /// <summary>
    ///     Layers with their own coloring type and properties
    /// </summary>
    [DataField("layers", true)]
    public Dictionary<string, LayerColoring>? Layers;
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
        MarkingSet markingSet
    )
    {
        var colors = new List<Color>();

        // Coloring from default properties
        var defaultColor = prototype.Coloring.Default.GetColor(skinColor, eyeColor, markingSet);

        if (prototype.Coloring.Layers == null)
        {
            // If layers is not specified, then every layer must be default
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                colors.Add(defaultColor);
            }
            return colors;
        }
        else
        {
            // If some layers are specified.
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                // Getting layer name
                string? name = prototype.Sprites[i] switch
                {
                    SpriteSpecifier.Rsi rsi => rsi.RsiState,
                    SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
                    _ => null
                };
                if (name == null)
                {
                    colors.Add(defaultColor);
                    continue;
                }
            
                // All specified layers must be colored separately, all unspecified must depend on default coloring
                if (prototype.Coloring.Layers.TryGetValue(name, out var layerColoring))
                {
                    var marking_color = layerColoring.GetColor(skinColor, eyeColor, markingSet);
                    colors.Add(marking_color);
                }
                else
                {
                    colors.Add(defaultColor);
                }
            }
            return colors;
        }
    }

    public static Color? AnyHairColor(Color? skinColor, Color? hairColor, Color? facialHairColor)
    {
        return hairColor ?? facialHairColor ?? skinColor;
    }

    public static Color? HairColor(Color? skinColor, Color? hairColor)
    {
        return hairColor ?? skinColor;
    }

    public static Color? FacialHairColor(Color? skinColor, Color? facialHairColor)
    {
        return facialHairColor ?? skinColor;
    }

    public static Color? SkinColor(Color? skinColor)
    {
        return skinColor;
    }
    
    public static Color? Tattoo(Color? skinColor)
    {
        if (skinColor == null) return null;

        var newColor = Color.ToHsv(skinColor.Value);
        newColor.Z = .20f;

        return Color.FromHsv(newColor);
    }

    public static Color? EyeColor(Color? eyeColor)
    {
        return eyeColor;
    }

    public static Color? SimpleColor(Color? color)
    {
        return color ?? new Color(1f, 1f, 1f, 1f);
    }
}

using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Default colors for marking
/// </summary>
[DataDefinition]
public sealed partial class MarkingColors
{
    /// <summary>
    /// Coloring properties that will be used on any unspecified layer
    /// </summary>
    [DataField("default", true)]
    public LayerColoringDefinition Default = new LayerColoringDefinition();

    /// <summary>
    ///     Layers with their own coloring type and properties
    /// </summary>
    [DataField("layers", true)]
    [Obsolete("Marking coloration should be defined in the Layers themselves instead.")]
    public Dictionary<string, LayerColoringDefinition>? Layers;
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
        List<Marking> otherMarkings
    )
    {
        var colors = new List<Color>();
        var defaultColor = prototype.Coloring.Default.GetColor(skinColor, eyeColor, otherMarkings);

        if (prototype.UsesLayers())
            GetColorsForMarkingLayers(prototype,
                ref colors,
                skinColor,
                eyeColor,
                otherMarkings,
                defaultColor);
        else
            GetColorsForOldSpriteLayers(prototype,
                ref colors,
                skinColor,
                eyeColor,
                otherMarkings,
                defaultColor);

        return colors;
    }

    /// <summary>
    ///     Gets a list of default fallback colors for the layers of a marking prototype.
    /// </summary>
    /// <param name="prototype">The marking prototype.</param>
    /// <param name="colors">A list of colors to populate.</param>
    /// <param name="skinColor">The skin color of the character.</param>
    /// <param name="eyeColor">The eye color of the character.</param>
    /// <param name="otherMarkings">A list of markings thie character has.</param>
    /// <param name="defaultColor">The universal default color for this marking.</param>
    private static void GetColorsForMarkingLayers(MarkingPrototype prototype,
        ref List<Color> colors,
        Color? skinColor,
        Color? eyeColor,
        List<Marking> otherMarkings,
        Color defaultColor)
    {
        var layers = prototype.Coloring.Layers;

        for (var i = 0; i < prototype.Layers.Count; i++)
        {
            var layer = prototype.Layers[i];
            var layerId = layer.GetLayerStateId();
            var color = defaultColor;

            // Color type associated with layer
            if (layer.Coloring != null)
                color = layer.Coloring.GetColor(skinColor, eyeColor, otherMarkings);

            // Color type associated with deprecated coloring field
            else if (layers != null && layers.TryGetValue(layerId, out var layerColoring))
                color = layerColoring.GetColor(skinColor, eyeColor, otherMarkings);

            colors.Add(color);
        }
    }

    [Obsolete("Function exists for the sake of deprecating MarkingPrototype.Sprites. Do not rely on it for future work.")]
    private static void GetColorsForOldSpriteLayers(MarkingPrototype prototype,
        ref List<Color> colors,
        Color? skinColor,
        Color? eyeColor,
        List<Marking> otherMarkings,
        Color defaultColor)
    {
        var layers = prototype.Coloring.Layers;

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
            if (layers != null && layers.TryGetValue(name, out var layerColoring))
            {
                var marking_color = layerColoring.GetColor(skinColor, eyeColor, otherMarkings);
                colors.Add(marking_color);
            }
            else
            {
                colors.Add(defaultColor);
            }
        }
    }
}

/// <summary>
///     A class that defines coloring type and fallback for markings
/// </summary>
[DataDefinition]
public sealed partial class LayerColoringDefinition
{
    [DataField("type")]
    public LayerColoringType? Type = new ColoringTypes.SkinColoring();

    /// <summary>
    ///     Coloring types that will be used if main coloring type will return nil
    /// </summary>
    [DataField("fallbackTypes")]
    public List<LayerColoringType> FallbackTypes = new() {};

    /// <summary>
    ///     Color that will be used if coloring type and fallback type will return nil
    /// </summary>
    [DataField("fallbackColor")]
    public Color FallbackColor = Color.White;

    public Color GetColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        Color? color = null;
        if (Type != null)
            color = Type.GetColor(skin, eyes, otherMarkings);
        if (color == null)
        {
            foreach (var type in FallbackTypes)
            {
                color = type.GetColor(skin, eyes, otherMarkings);
                if (color != null) break;
            }
        }
        return color ?? FallbackColor;
    }
}

/// <summary>
///     An abstract class for coloring types
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class LayerColoringType
{
    /// <summary>
    ///     Makes output color negative
    /// </summary>
    [DataField("negative")]
    public bool Negative { get; private set; } = false;
    public abstract Color? GetCleanColor(Color? skin, Color? eyes, List<Marking> otherMarkings);
    public Color? GetColor(Color? skin, Color? eyes, List<Marking> otherMarkings)
    {
        var color = GetCleanColor(skin, eyes, otherMarkings);
        // Negative color
        if (color != null && Negative)
        {
            var rcolor = color.Value;
            rcolor.R = 1f-rcolor.R;
            rcolor.G = 1f-rcolor.G;
            rcolor.B = 1f-rcolor.B;
            return rcolor;
        }
        return color;
    }
}

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
    [Obsolete("Marking coloration should be defined in the layers' MarkingLayerData instead.")]
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
        var defaultColor = prototype.Coloring.Default.GetColor(skinColor, eyeColor, otherMarkings);

        var colors = GetColorsForMarkingLayers(prototype,
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
    /// <param name="skinColor">The skin color of the character.</param>
    /// <param name="eyeColor">The eye color of the character.</param>
    /// <param name="otherMarkings">A list of markings thie character has.</param>
    /// <param name="defaultColor">The universal default color for this marking.</param>
    private static List<Color> GetColorsForMarkingLayers(MarkingPrototype prototype,
        Color? skinColor,
        Color? eyeColor,
        List<Marking> otherMarkings,
        Color defaultColor)
    {
        var colors = new List<Color>();
        var layers = prototype.Coloring.Layers;

        for (var i = 0; i < prototype.Sprites.Count; i++)
        {
            var layer = prototype.Sprites[i];
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

        return colors;
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

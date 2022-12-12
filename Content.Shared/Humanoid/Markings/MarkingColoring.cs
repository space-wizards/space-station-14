using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Humanoid.Markings;

[Serializable, NetSerializable]
public enum MarkingColoringType : byte
{
    Color,
    FollowSkinColor,
    FollowHairColor,
    FollowFacialHairColor,
    FollowAnyHairColor,
    FollowEyeColor
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
    public List<MarkingColorEntry>? Layers;
}

/// <summary>
/// Prototype entry to layer coloring
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed class MarkingColorEntry
{
    /// <summary>
    /// Name of sprite layer
    /// </summary>
    [DataField("name", true, required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Coloring properties
    /// </summary>
    [DataField("properties", true, required: true)]
    public ColoringProperties Properties { get; } = default!;
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
    public MarkingColoringType Type { get; } = MarkingColoringType.FollowSkinColor;

    /// <summary>
    /// Color that will be used if coloring type can't be performed
    /// </summary>
    [DataField("color", true)]
    public Color Color { get; } = new (1f, 1f, 1f, 1f);

    /// <summary>
    /// Makes color negative if true (in e.x. Different Eyes)
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
    public static Color GetMarkingColor(MarkingColoringType type, Color? skinColor, Color? eyeColor, Color? hairColor, Color? facialHairColor, Color? defaultColor, bool negative = false)
    {
        Color? color = defaultColor;
        switch (type)
        {
            case MarkingColoringType.Color:
                color = Color();
                break;
            case MarkingColoringType.FollowSkinColor:
                color = FollowSkinColor(skinColor);
                break;
            case MarkingColoringType.FollowHairColor:
                color = FollowHairColor(skinColor, hairColor);
                break;
            case MarkingColoringType.FollowFacialHairColor:
                color = FollowHairColor(skinColor, facialHairColor);
                break;
            case MarkingColoringType.FollowAnyHairColor:
                color = FollowAnyHairColor(skinColor, hairColor, facialHairColor);
                break;
            case MarkingColoringType.FollowEyeColor:
                color = FollowEyeColor(eyeColor);
                break;
        }

        return color ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FollowAnyHairColor(Color? skinColor, Color? hairColor, Color? facialHairColor)
    {
        return hairColor ?? facialHairColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FollowHairColor(Color? skinColor, Color? hairColor)
    {
        return hairColor ?? skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FollowFacialHairColor(Color? skinColor, Color? facialHairColor)
    {
        return facialHairColor ?? skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FollowSkinColor(Color? skinColor)
    {
        return skinColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color FollowEyeColor(Color? eyeColor)
    {
        return eyeColor ?? new (1f, 1f, 1f, 1f);
    }

    public static Color Color()
    {
        return new Color(1f, 1f, 1f, 1f);
    }
}

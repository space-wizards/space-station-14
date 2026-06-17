using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     A representation of properties associated with a single layer of a marking.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial record MarkingLayerData
{
    /// <summary>
    ///     The sprite associated with this marking layer.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    /// <summary>
    ///     A localization ID for this marking layer.
    /// </summary>
    [DataField]
    public LocId? Name = null;

    /// <summary>
    ///     The default coloring that this layer uses.
    /// </summary>
    [DataField]
    public LayerColoringDefinition Coloring = new();

    /// <summary>
    ///     If this is true, then this layer cannot be recolored in the character editor.
    /// </summary>
    [DataField]
    public bool ForcedColoring = false;

    /// <summary>
    ///     Tries to get a player-friendly name for this marking layer.
    /// </summary>
    /// <param name="markingId">The ID of the marking this layer belongs to.</param>
    public string GetLayerName(string markingId)
    {
        var locId = GetLocId(markingId);
        return Loc.GetString(locId);
    }

    /// <summary>
    ///     Gets a locale ID for this marking layer. Uses <see cref="Name"/> by default,
    ///     otherwise uses an implicit key based on the marking ID and the layer ID.
    /// </summary>
    /// <param name="markingId">The ID of the marking this layer belongs to.</param>
    public string GetLocId(string markingId)
    {
        return Name ?? $"marking-{markingId}-{GetLayerStateId()}";
    }

    /// <summary>
    ///     Gets an ID associated with this marking layer. Used for sprite keys.
    /// </summary>
    /// <param name="markingId">The ID of the marking this layer belongs to.</param>
    public string GetLayerID(string markingId)
    {
        return $"{markingId}-{GetLayerStateId()}";
    }

    /// <summary>
    ///     Get the ID associated with this marking's layer state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the SpriteSpecifier is an unknown type.</exception>
    public string GetLayerStateId()
    {
        return Sprite switch
        {
            SpriteSpecifier.Rsi rsi => rsi.RsiState,
            SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
            _ => throw new InvalidOperationException("SpriteSpecifier not of known type"),
        };
    }
}

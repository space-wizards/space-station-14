using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Prototypes;

[Prototype]
public sealed partial class NavMapBlipPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Sets whether the associated entity can be selected when the blip is clicked
    /// </summary>
    [DataField]
    public bool Selectable = false;

    /// <summary>
    /// Sets whether the blips is always blinking
    /// </summary>
    [DataField]
    public bool Blinks = false;

    /// <summary>
    /// Sets the color of the blip
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.LightGray;

    /// <summary>
    /// Texture paths associated with the blip
    /// </summary>
    [DataField]
    public ResPath[]? TexturePaths { get; private set; }

    /// <summary>
    /// Sets the UI scaling of the blip
    /// </summary>
    [DataField]
    public float Scale { get; private set; } = 1f;

    /// <summary>
    /// Describes how the blip should be positioned.
    /// It's up to the individual system to enforce this
    /// </summary>
    [DataField]
    public NavMapBlipPlacement Placement { get; private set; } = NavMapBlipPlacement.Centered;
}

public enum NavMapBlipPlacement
{
    Centered,   // The blip appears in the center of the tile
    Offset      // The blip is offset from the center of the tile (determined by the system using the blips)
}

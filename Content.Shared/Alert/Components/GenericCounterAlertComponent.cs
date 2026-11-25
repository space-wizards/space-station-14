using Robust.Shared.GameStates;

namespace Content.Shared.Alert.Components;

/// <summary>
/// This is used for an alert which simply displays a generic number over a texture.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GenericCounterAlertComponent : Component
{
    /// <summary>
    /// The width, in pixels, of an individual glyph, accounting for the space between glyphs.
    /// A 3 pixel wide glyph with one pixel of space between it and the next would be a width of 4.
    /// </summary>
    [DataField]
    public int GlyphWidth = 6;

    /// <summary>
    /// Whether the numbers should be centered on the glyph or just follow a static position.
    /// </summary>
    [DataField]
    public bool CenterGlyph = true;

    /// <summary>
    /// Whether leading zeros should be hidden.
    /// If true, "005" would display as "5".
    /// </summary>
    [DataField]
    public bool HideLeadingZeroes = true;

    /// <summary>
    /// The size of the alert sprite.
    /// Used to calculate offsets.
    /// </summary>
    [DataField]
    public Vector2i AlertSize = new(32, 32);

    /// <summary>
    /// Digits that can be displayed by the alert, represented by their sprite layer.
    /// Order defined corresponds to the digit it affects. 1st defined will affect 1st digit, 2nd affect 2nd digit and so on.
    /// In this case ones would be on layer "1", tens on layer "10" etc.
    /// </summary>
    [DataField]
    public List<string> DigitKeys = new()
    {
        "1",
        "10",
        "100",
        "1000",
        "10000"
    };
}

/// <summary>
/// Event raised to gather the amount the alert will display.
/// </summary>
/// <param name="Alert">The alert which is currently requesting an update.</param>
/// <param name="Amount">The number to display on the alert.</param>
[ByRefEvent]
public record struct GetGenericAlertCounterAmountEvent(AlertPrototype Alert, int? Amount = null)
{
    public bool Handled => Amount.HasValue;
}

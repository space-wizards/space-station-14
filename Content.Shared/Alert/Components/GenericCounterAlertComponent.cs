using Robust.Shared.GameStates;

namespace Content.Shared.Alert.Components;

/// <summary>
/// This is used for an alert which simply displays a generic number
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

    [DataField]
    public bool CenterGlyph = true;

    [DataField]
    public bool HideLeadingZeroes = true;

    [DataField]
    public Vector2i AlertSize = new(32, 32);

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

[ByRefEvent]
public record struct GetGenericAlertCounterAmountEvent(int? Amount = null)
{
    public bool Handled => Amount != null;
}

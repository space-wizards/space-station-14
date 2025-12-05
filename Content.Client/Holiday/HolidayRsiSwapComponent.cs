using Content.Shared.Holiday;

namespace Content.Client.Holiday;

/// <summary>
/// This is used for a component that swaps an entity's sprite RSI based on <see cref="HolidayVisualsComponent"/>.
/// </summary>
/// <remarks> Causes a race condition if there are overlapping holidays. </remarks>
[RegisterComponent]
public sealed partial class HolidayRsiSwapComponent : Component
{
    /// <summary>
    /// A dictionary of keys on <see cref="HolidayVisuals.Holiday"/> associated to an rsi.
    /// </summary>
    [DataField(required:true)]
    public Dictionary<string, string> Sprite = new();
}

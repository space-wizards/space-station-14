using Robust.Client.GameObjects;

namespace Content.Client.Holiday;

/// <summary>
/// This is used for a component that swaps an entity's RSI based on HolidayVisuals
/// </summary>
/// <remarks> Causes a race condition if there are overlapping holidays. </remarks>
[RegisterComponent]
public sealed partial class HolidayRsiSwapComponent : Component
{
    /// <summary>
    /// A dictionary of arbitrary visual keys to an rsi to swap the sprite to.
    /// </summary>
    [DataField]
    public Dictionary<string, string> Sprite = new();

    /// <summary>
    /// Rsi to swap to for if the holiday ends.
    /// </summary>
    [DataField]
    public string Default;
}

namespace Content.Server._Tinystation.Speech.Components;

/// <summary>
///     Rattle me bones!
/// </summary>
[RegisterComponent]
public sealed partial class KnightAccentComponent : Component
{
    /// <summary>
    ///     Chance that the message will be appended with "ACK ACK!"
    /// </summary>
    [DataField("ackChance")]
    public float ackChance = 0.3f;
}

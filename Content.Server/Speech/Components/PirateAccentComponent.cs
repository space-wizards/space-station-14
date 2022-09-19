namespace Content.Server.Speech.Components;

/// <summary>
///     Nyehh, my gabagool, see?
///     Etc etc.
/// </summary>
[RegisterComponent]
public sealed class PirateAccentComponent : Component
{
    /// <summary>
    ///     Do you make all the rules?
    /// </summary>
    [DataField("isPirate")]
    public bool IsPirate = true;
}

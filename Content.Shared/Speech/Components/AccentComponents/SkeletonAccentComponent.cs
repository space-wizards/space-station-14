using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components.AccentComponents;

/// <summary>
/// Rattle me bones!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SkeletonAccentComponent : Component
{
    /// <summary>
    /// Chance that the message will be appended with "ACK ACK!"
    /// </summary>
    [DataField]
    public float AckChance = 0.3f; // Funnier if it doesn't happen every single time
}

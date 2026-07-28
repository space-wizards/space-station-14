using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Rattle me bones!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SkeletonAccentSystem))]
public sealed partial class SkeletonAccentComponent : BaseAccentComponent
{
    /// <summary>
    /// Chance that the message will be appended with "ACK ACK!"
    /// </summary>
    [DataField]
    public float AckChance = 0.3f; // Funnier if it doesn't happen every single time
}

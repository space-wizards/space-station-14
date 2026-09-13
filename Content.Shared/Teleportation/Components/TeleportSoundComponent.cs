using Robust.Shared.Audio;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Plays sounds at a target's departure and arrival locations during teleportation.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportSoundComponent : Component
{
    /// <summary>
    /// Sound played at the target's departure location.
    /// </summary>
    [DataField]
    public SoundSpecifier? DepartureSound;

    /// <summary>
    /// Sound played at the target's arrival location.
    /// </summary>
    [DataField]
    public SoundSpecifier? ArrivalSound;
}

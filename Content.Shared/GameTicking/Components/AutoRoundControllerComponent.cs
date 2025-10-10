using Robust.Shared.GameObjects;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Map-placed controller that enables automatic round end/restart timings.
/// This stub satisfies server systems that query and read its configuration.
/// </summary>
[RegisterComponent]
public sealed partial class AutoRoundControllerComponent : Component
{
    // Master enable flags
    [DataField] public bool EnableInRoundAutoEnd = true;
    [DataField] public bool EnablePostRoundAutoRestart = true;

    // In-round auto end
    [DataField] public float InRoundDelay = 180f;
    [DataField] public float InRoundWarnThreshold = 30f;

    // Post-round auto restart
    [DataField] public float PostRoundDelay = 15f;
    [DataField] public float PostRoundWarnThreshold = 5f;

    // Announcer name
    [DataField] public string SenderName = "Мировая арена";
}

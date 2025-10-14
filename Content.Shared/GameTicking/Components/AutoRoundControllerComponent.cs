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
    // Announcement template for in-round warning. Use {remaining} placeholder for seconds.
    [DataField] public string InRoundWarnMessage = ""; // set in prototype, empty = no announcement

    // Post-round auto restart
    [DataField] public float PostRoundDelay = 15f;
    [DataField] public float PostRoundWarnThreshold = 5f;
    // Announcement template for post-round warning. Use {remaining} placeholder.
    [DataField] public string PostRoundWarnMessage = ""; // set in prototype, empty = no announcement

    // Announcer name
    [DataField] public string SenderName = ""; // set in prototype
}

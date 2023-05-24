namespace Content.Client.AME.Components;

[RegisterComponent]
public sealed class AmeShieldingVisualsComponent : Component
{
    /// <summary>
    /// The sprite state used when the AME shielding is acting as a core for the AME and the AME is injecting fuel at a safe rate.
    /// </summary>
    [DataField("stableState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string StableState = "core_weak";

    /// <summary>
    /// The sprite state used when the AME shielding is acting as a core for the AME and the AME is injecting fuel at an unsafe rate.
    /// </summary>
    [DataField("unstableState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string UnstableState = "core_strong";
}

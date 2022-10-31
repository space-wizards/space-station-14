using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for making something blind forever.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class HastyComponent : Component
{
    [DataField("movementSpeedMultiplier", required: true)]
    public float MovementSpeedMultiplier { get; }
    [DataField("trySlipInterval", required: true)]
    public float TrySlipInterval { get; }
    [DataField("chanceOfSlip", required: true)]
    public float ChanceOfSlip { get; }
    [DataField("launchForwardsMultiplier", required: true)]
    public float LaunchForwardsMultiplier { get; }
    [DataField("stunDuration", required: true)]
    public Vector2 StunDuration;

    public DateTime LastSlipAttemptTime;
}


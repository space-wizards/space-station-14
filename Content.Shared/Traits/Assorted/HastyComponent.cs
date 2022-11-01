using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

[RegisterComponent, NetworkedComponent]
public sealed class HastyComponent : Component
{
    /// <summary>
    /// How much faster will the player move?
    /// </summary>
    [DataField("movementSpeedMultiplier", required: true)]
    public float MovementSpeedMultiplier { get; }
    /// <summary>
    /// When moving: the time between trying to slip the user
    /// </summary>
    [DataField("trySlipInterval", required: true)]
    public float TrySlipInterval { get; }
    /// <summary>
    /// When moving: the chance of slipping (from 0.0 to 1.0)
    /// </summary>
    [DataField("chanceOfSlip", required: true)]
    public float ChanceOfSlip { get; }
    /// <summary>
    /// The distance the player will be launched when slipping
    /// </summary>
    [DataField("launchForwardsMultiplier", required: true)]
    public float LaunchForwardsMultiplier { get; }
    /// <summary>
    /// The duration of the stuns
    /// </summary>
    [DataField("stunDuration", required: true)]
    public Vector2 StunDuration;

    public DateTime LastSlipAttemptTime;
}


namespace Content.Shared.Defects.Components;

/// <summary>
/// Randomizes the fuse delay on a <c>TimerTriggerComponent</c> at spawn.
/// Each instance rolls a delay in [MinDelay, MaxDelay] seconds.
/// </summary>
[RegisterComponent]
public sealed partial class RandomFuseComponent : DefectComponent
{
    /// <summary>Minimum fuse delay in seconds (inclusive).</summary>
    [DataField]
    public float MinDelay = 2.0f;

    /// <summary>Maximum fuse delay in seconds (inclusive).</summary>
    [DataField]
    public float MaxDelay = 6.0f;
}

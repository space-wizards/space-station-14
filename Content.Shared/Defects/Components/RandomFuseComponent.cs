namespace Content.Shared.Defects.Components;

// Randomizes the fuse delay on a TimerTriggerComponent at spawn.
// Each instance rolls a delay in [MinDelay, MaxDelay] seconds.
[RegisterComponent]
public sealed partial class RandomFuseComponent : DefectComponent
{
    // Minimum fuse delay in seconds (inclusive).
    [DataField]
    public float MinDelay = 2.0f;

    // Maximum fuse delay in seconds (inclusive).
    [DataField]
    public float MaxDelay = 6.0f;
}

namespace Content.Server.Explosion.Components;

/// <summary>
/// This is used for randomizing a <see cref="RandomTimerTriggerComponent"/> on MapInit
/// </summary>
[RegisterComponent]
public sealed partial class RandomTimerTriggerComponent : Component
{
    /// <summary>
    /// The minimum trigger time.
    /// </summary>
    [DataField]
    public float Min;

    /// <summary>
    /// The maximum trigger time
    /// </summary>
    [DataField]
    public float Max;
}


namespace Content.Server.Tesla.Components;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
[RegisterComponent]
public sealed partial class TeslaGroundingRodComponent : Component
{
    /// <summary>
    /// Spark duration.
    /// </summary>
    [DataField]
    public TimeSpan LightningTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// When the spark visual should turn off.
    /// </summary>
    public TimeSpan LightningEndTime;

    public bool IsSparking;

    /// <summary>
    /// priority for lightning strikes when the device is powered up
    /// </summary>
    [DataField]
    public int EnabledPriority = 3;

    /// <summary>
    /// priority for lightning strikes when the device is turned off
    /// </summary>
    [DataField]
    public int DisabledPriority = 0;
}

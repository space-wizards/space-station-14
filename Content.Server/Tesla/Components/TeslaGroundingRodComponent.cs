
using Robust.Shared.Audio;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Depending on the energy it receives, it changes its priority of selecting a lightning target
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

    public bool Enabled;

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

    [DataField]
    public SoundSpecifier SoundOpen = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField]
    public SoundSpecifier SoundClose = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}


using Robust.Shared.Audio;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
[RegisterComponent]
public sealed partial class TeslaCoilComponent : Component
{
    /// <summary>
    /// How much power will the coil generate from a lightning strike
    /// </summary>
    // To Do: Different lightning bolts have different powers and generate different amounts of energy
    [DataField]
    public float ChargeFromLightning = 30000f;

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
    /// Was machine activated by user?
    /// </summary>
    [DataField]
    public bool Enabled;

    [DataField]
    public SoundSpecifier SoundOpen = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField]
    public SoundSpecifier SoundClose = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}

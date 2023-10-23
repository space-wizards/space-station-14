using Content.Server.Tesla.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Depending on the energy it receives, it changes its priority of selecting a lightning target
/// </summary>
[RegisterComponent, Access(typeof(TeslaGroundingRodSystem))]
public sealed partial class TeslaGroundingRodComponent : Component
{
    /// <summary>
    /// Spark duration.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LightningTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// When the spark visual should turn off.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LightningEndTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsSparking;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled;

    /// <summary>
    /// priority for lightning strikes when the device is powered up
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int EnabledPriority = 3;

    /// <summary>
    /// priority for lightning strikes when the device is turned off
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DisabledPriority = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundOpen = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundClose = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}

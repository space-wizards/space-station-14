using Content.Server.Power.NodeGroups;
using Content.Shared.APC;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ApcComponent : BaseApcNetComponent
{
    [DataField("onReceiveMessageSound")]
    public SoundSpecifier OnReceiveMessageSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    public ApcChargeState LastChargeState;
    public TimeSpan? LastChargeStateTime;

    public ApcExternalPowerState LastExternalState;

    /// <summary>
    /// Time the ui was last updated automatically.
    /// Done after every <see cref="VisualsChangeDelay"/> to show the latest load.
    /// If charge state changes it will be instantly updated.
    /// </summary>
    public TimeSpan LastUiUpdate;

    [DataField("enabled")]
    public bool MainBreakerEnabled = true;

    /// <summary>
    /// APC state needs to always be updated after first processing tick.
    /// </summary>
    public bool NeedStateUpdate;

    public const float HighPowerThreshold = 0.9f;
    public static TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum continuous load in Watts that this APC can supply to loads. Exceeding this starts a
    /// timer, which after enough overloading causes the APC to "trip" off.
    /// </summary>
    [DataField]
    public float MaxLoad = 20e3f;

    /// <summary>
    /// Time that the APC can be continuously overloaded before tripping off.
    /// </summary>
    [DataField]
    public TimeSpan TripTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Time that overloading began.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? TripStartTime;

    /// <summary>
    /// Set to true if the APC tripped off. Used to indicate problems in the UI. Reset by switching
    /// APC on.
    /// </summary>
    [DataField]
    public bool TripFlag;

    // TODO ECS power a little better!
    // End the suffering
    protected override void AddSelfToNet(IApcNet apcNet)
    {
        apcNet.AddApc(Owner, this);
    }

    protected override void RemoveSelfFromNet(IApcNet apcNet)
    {
        apcNet.RemoveApc(Owner, this);
    }
}

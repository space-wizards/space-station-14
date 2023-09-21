using Content.Server.Power.NodeGroups;
using Content.Shared.APC;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class ApcComponent : BaseApcNetComponent
{
    [DataField("onReceiveMessageSound")]
    public SoundSpecifier OnReceiveMessageSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField("lastChargeState")]
    public ApcChargeState LastChargeState;
    [DataField("lastChargeStateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastChargeStateTime;

    /// <summary>
    /// Time the ui was last updated automatically.
    /// Done after every <see cref="VisualsChangeDelay"/> to show the latest load.
    /// If charge state changes it will be instantly updated.
    /// </summary>
    [DataField("lastUiUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastUiUpdate;

    [DataField("enabled")]
    public bool MainBreakerEnabled = true;

    public const float HighPowerThreshold = 0.9f;
    public static TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

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

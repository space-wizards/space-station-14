using Content.Server.Power.NodeGroups;
using Content.Shared.APC;
using Robust.Shared.Audio;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class ApcComponent : BaseApcNetComponent
{
    [DataField("onReceiveMessageSound")]
    public SoundSpecifier OnReceiveMessageSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [ViewVariables]
    public ApcChargeState LastChargeState;
    public TimeSpan LastChargeStateTime;

    [ViewVariables]
    public ApcExternalPowerState LastExternalState;
    public TimeSpan LastUiUpdate;

    [ViewVariables]
    public bool MainBreakerEnabled = true;
    public bool HasAccess = false;

    public const float HighPowerThreshold = 0.9f;
    public static TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

    // TODO ECS power a little better!
    protected override void AddSelfToNet(IApcNet apcNet)
    {
        apcNet.AddApc(this);
    }

    protected override void RemoveSelfFromNet(IApcNet apcNet)
    {
        apcNet.RemoveApc(this);
    }
}

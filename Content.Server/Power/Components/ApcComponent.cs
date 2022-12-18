using Content.Server.Power.NodeGroups;
using Content.Shared.APC;
using Robust.Shared.Audio;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed class ApcComponent : BaseApcNetComponent
{
    [DataField("onReceiveMessageSound")]
    public SoundSpecifier OnReceiveMessageSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [ViewVariables]
    public ApcChargeState LastChargeState;
    public TimeSpan LastChargeStateTime;

    /// <summary>
    ///     Is the panel open for this entity's APC?
    /// </summary>
    [DataField("open")]
    public bool IsApcOpen { get; set; }

    [ViewVariables]
    public ApcExternalPowerState LastExternalState;
    public TimeSpan LastUiUpdate;

    [ViewVariables]
    public bool MainBreakerEnabled = true;

    public bool Emagged = false;

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

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");

}

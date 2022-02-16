using System;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared.APC;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components;

[RegisterComponent]
[Friend(typeof(ApcSystem))]
public sealed class ApcComponent : BaseApcNetComponent
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

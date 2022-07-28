using Content.Server.Power.Components;

namespace Content.Server.Radio.Components.Telecomms;

public abstract class TelecommsMachine : Component
{
    /// <summary>
    /// The network for this machine.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("network")]
    public string Network { get; set; } = default!;

    /// <summary>
    /// How much traffic to lose per tick (50 gigabytes/second * netspeed)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("netspeed")]
    public int Netspeed { get; } = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Traffic { get; set; }

    /// <remarks>If no <see cref="ApcPowerReceiverComponent"/> is found, it's assumed power is not required.</remarks>
    [ViewVariables]
    public bool CanRun => _powerReceiver is null || _powerReceiver.Powered;

    private ApcPowerReceiverComponent? _powerReceiver;

    protected override void Initialize()
    {
        base.Initialize();
        IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out _powerReceiver);
    }
}

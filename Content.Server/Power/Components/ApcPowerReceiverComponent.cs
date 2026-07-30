using Content.Server.Power.Pow3r;
using Content.Shared.Power.Components;

namespace Content.Server.Power.Components;

/// <inheritdoc />
[RegisterComponent]
public sealed partial class ApcPowerReceiverComponent : SharedApcPowerReceiverComponent
{
    /// <inheritdoc />
    public override float Load
    {
        get => NetworkLoad.DesiredPower;
        set => NetworkLoad.DesiredPower = value;
    }

    public ApcPowerProviderComponent? Provider = null;

    /// <inheritdoc />
    public override bool PowerDisabled
    {
        get => !NetworkLoad.Enabled;
        set => NetworkLoad.Enabled = !value;
    }

    [ViewVariables]
    public PowerState.Load NetworkLoad { get; } = new PowerState.Load
    {
        DesiredPower = 5
    };

    public float PowerReceived => NetworkLoad.ReceivingPower;
}


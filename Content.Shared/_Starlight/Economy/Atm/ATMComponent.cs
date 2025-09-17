using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Economy.Atm;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ATMComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier WithdrawSound = new SoundPathSpecifier("/Audio/_Starlight/Misc/atm.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier DepositSound = new SoundPathSpecifier("/Audio/_Starlight/Misc/atm_in.ogg");
}

[Serializable, NetSerializable]
public enum ATMUIKey
{
    Key
}
[Serializable, NetSerializable]
public sealed class ATMBuiState : BoundUserInterfaceState
{
    public int Balance { get; init; }
    public string? Message { get; init; }
    public bool IsError { get; init; }
}
[RegisterComponent, NetworkedComponent]
public sealed partial class NTCashComponent : Component
{

}
[Serializable, NetSerializable]
public sealed class ATMWithdrawBuiMsg : BoundUserInterfaceMessage
{
    public int Amount { get; init; }
}

[Serializable, NetSerializable]
public sealed class ATMTransferBuiMsg : BoundUserInterfaceMessage
{
    public string Recipient { get; init; } = string.Empty;

    public int Amount { get; init; }
}
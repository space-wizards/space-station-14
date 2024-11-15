using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Economy.Atm;

[RegisterComponent, NetworkedComponent]
public sealed partial class ATMComponent : Component
{
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
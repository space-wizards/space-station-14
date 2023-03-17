using Robust.Shared.GameStates;

namespace Content.Shared.Bank
{
    [RegisterComponent, NetworkedComponent]
    public sealed class BankAccountComponent : Component
    {
        [DataField("balance")]
        public int Balance;
    }
}

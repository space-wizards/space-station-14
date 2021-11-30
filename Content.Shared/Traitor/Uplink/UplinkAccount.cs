using Robust.Shared.GameObjects;

namespace Content.Shared.Traitor.Uplink
{
    public class UplinkAccount
    {
        public readonly EntityUid? AccountHolder;
        public int Balance;

        public UplinkAccount(int startingBalance, EntityUid? accountHolder = null)
        {
            AccountHolder = accountHolder;
            Balance = startingBalance;
        }
    }
}

using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;

namespace Content.Server.Traitor.Uplink.Account
{
    public class UplinkAccountBalanceChanged : EntityEventArgs
    {
        public readonly UplinkAccount Account;
        public readonly int Difference;

        public readonly int NewBalance;
        public readonly int OldBalance;

        public UplinkAccountBalanceChanged(UplinkAccount account, int difference)
        {
            Account = account;
            Difference = difference;

            NewBalance = account.Balance;
            OldBalance = account.Balance - difference;

        }
    }
}

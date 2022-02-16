using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;

namespace Content.Server.Traitor.Uplink.Account
{
    /// <summary>
    /// Invokes when one of the UplinkAccounts changed its TC balance
    /// </summary>
    public sealed class UplinkAccountBalanceChanged : EntityEventArgs
    {
        public readonly UplinkAccount Account;

        /// <summary>
        /// Difference between NewBalance - OldBalance
        /// </summary>
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

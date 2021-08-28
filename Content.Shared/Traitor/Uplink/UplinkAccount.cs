using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.Traitor.Uplink
{
    public class UplinkAccount
    {
        public event Action<UplinkAccount>? BalanceChanged;
        public EntityUid AccountHolder;
        private int _balance;
        [ViewVariables]
        public int Balance => _balance;

        public UplinkAccount(EntityUid uid, int startingBalance)
        {
            AccountHolder = uid;
            _balance = startingBalance;
        }

        public bool ModifyAccountBalance(int newBalance)
        {
            if (newBalance < 0)
            {
                return false;
            }
            _balance = newBalance;
            BalanceChanged?.Invoke(this);
            return true;
        }
    }
}

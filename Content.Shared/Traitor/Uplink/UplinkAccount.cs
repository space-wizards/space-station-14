using Robust.Shared.GameObjects;

namespace Content.Shared.Traitor.Uplink
{
    public class UplinkAccount
    {
        public readonly EntityUid AccountHolder;
        public int Balance;

        public UplinkAccount(EntityUid uid, int startingBalance)
        {
            AccountHolder = uid;
            Balance = startingBalance;
        }
    }
}

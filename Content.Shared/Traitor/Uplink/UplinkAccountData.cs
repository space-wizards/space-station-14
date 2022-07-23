using Content.Shared.Roles;
using Robust.Shared.Serialization;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkAccountData
    {
        public EntityUid? DataAccountHolder;
        public int DataBalance;
        public UplinkAccountData(EntityUid? dataAccountHolder, int dataBalance)
        {
            DataAccountHolder = dataAccountHolder;
            DataBalance = dataBalance;
        }
    }
}

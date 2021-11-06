using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public class UplinkAccountData
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

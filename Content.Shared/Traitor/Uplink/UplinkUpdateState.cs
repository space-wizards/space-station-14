using Robust.Shared.GameObjects;

namespace Content.Shared.Traitor.Uplink
{
    public class UplinkUpdateState : BoundUserInterfaceState
    {
        public UplinkAccountData Account = default!;
        public UplinkListingData[] Listings = default!;
    }
}

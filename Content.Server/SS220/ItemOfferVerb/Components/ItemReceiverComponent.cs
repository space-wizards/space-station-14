// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.ItemOfferVerb.Systems;

namespace Content.Server.SS220.ItemOfferVerb.Components
{
    [RegisterComponent]
    [Access(typeof(ItemOfferSystem))]
    public sealed partial class ItemReceiverComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid Giver { get; set; }
        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Item { get; set; }
        [ViewVariables(VVAccess.ReadOnly)]
        public float ReceiveRange = 2f;
    }
}
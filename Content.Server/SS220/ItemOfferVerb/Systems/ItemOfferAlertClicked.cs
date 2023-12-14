// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.ItemOfferVerb.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.SS220.ItemOfferVerb.Systems
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ItemOfferAlertClicked : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            if (entManager.TryGetComponent(player, out ItemReceiverComponent? itemReceiverComponent))
            {
                entManager.System<ItemOfferSystem>().TransferItemInHands(player, itemReceiverComponent); 
            }
        }
    }
}

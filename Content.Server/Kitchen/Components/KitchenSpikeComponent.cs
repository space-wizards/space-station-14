using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Popups;
using Content.Shared.DragDrop;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent, Friend(typeof(KitchenSpikeSystem))]
    public class KitchenSpikeComponent : SharedKitchenSpikeComponent, ISuicideAct
    {
        public int MeatParts;
        public string? MeatPrototype;

        // TODO: Spiking alive mobs? (Replace with uid) (deal damage to their limbs on spiking, kill on first butcher attempt?) 
        public string MeatSource1p = "?";
        public string MeatSource0 = "?";
        public string MeatName = "?";

        // ECS this out!, when DragDropSystem and InteractionSystem refactored
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }

        // ECS this out!, Handleable SuicideEvent?
        SuicideKind ISuicideAct.Suicide(EntityUid victim, IChatManager chat)
        {
            var othersMessage = Loc.GetString("comp-kitchen-spike-suicide-other", ("victim", victim));
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-kitchen-spike-suicide-self");
            victim.PopupMessage(selfMessage);

            return SuicideKind.Piercing;
        }
    }
}

using System.Collections.Generic;
using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Strip
{
    public sealed class StrippableSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StrippableComponent, GetVerbsEvent<Verb>>(AddStripVerb);
            SubscribeLocalEvent<StrippableComponent, DidEquipEvent>(OnDidEquip);
            SubscribeLocalEvent<StrippableComponent, DidUnequipEvent>(OnDidUnequip);
            SubscribeLocalEvent<StrippableComponent, ComponentInit>(OnCompInit);
        }

        private void OnCompInit(EntityUid uid, StrippableComponent component, ComponentInit args)
        {
            SendUpdate(uid, component);
        }

        private void OnDidUnequip(EntityUid uid, StrippableComponent component, DidUnequipEvent args)
        {
            SendUpdate(uid, component);
        }

        private void OnDidEquip(EntityUid uid, StrippableComponent component, DidEquipEvent args)
        {
            SendUpdate(uid, component);
        }

        public void SendUpdate(EntityUid uid, StrippableComponent? strippableComponent = null)
        {
            if (!Resolve(uid, ref strippableComponent, false) || strippableComponent.UserInterface == null)
            {
                return;
            }

            var cuffs = new Dictionary<EntityUid, string>();
            var inventory = new Dictionary<(string ID, string Name), string>();
            var hands = new Dictionary<string, string>();

            if (TryComp(uid, out CuffableComponent? cuffed))
            {
                foreach (var entity in cuffed.StoredEntities)
                {
                    var name = Name(entity);
                    cuffs.Add(entity, name);
                }
            }

            if (_inventorySystem.TryGetSlots(uid, out var slots))
            {
                foreach (var slot in slots)
                {
                    var name = "None";

                    if (_inventorySystem.TryGetSlotEntity(uid, slot.Name, out var item))
                        name = Name(item.Value);

                    inventory[(slot.Name, slot.DisplayName)] = name;
                }
            }

            if (TryComp(uid, out HandsComponent? handsComp))
            {
                foreach (var hand in handsComp.HandNames)
                {
                    var owner = handsComp.GetItem(hand)?.Owner;

                    if (!owner.HasValue || HasComp<HandVirtualItemComponent>(owner.Value))
                    {
                        hands[hand] = "None";
                        continue;
                    }

                    hands[hand] = Name(owner.Value);
                }
            }

            strippableComponent.UserInterface.SetState(new StrippingBoundUserInterfaceState(inventory, hands, cuffs));
        }

        private void AddStripVerb(EntityUid uid, StrippableComponent component, GetVerbsEvent<Verb> args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract || args.Target == args.User)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("strip-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png";
            verb.Act = () => component.OpenUserInterface(actor.PlayerSession);
            args.Verbs.Add(verb);
        }
    }
}

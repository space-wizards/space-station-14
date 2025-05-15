using Content.Shared.Actions;
using Content.Shared.Chasm;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Whistle;
using Robust.Shared.Timing;
using System;
using Content.Shared.Chat;

namespace Content.Shared.Clothing.EntitySystems
{
    public abstract class SharedSecurityHailerSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<ActionSecHailerActionEvent>(OnHailOrder);
        }

        private void OnHailOrder(ActionSecHailerActionEvent ev)
        {
            //Exclamation point code here so it can be predicted

            Log.Debug("OnHailOrder exclamation point !");
        }

        private void OnEquip(EntityUid uid, SecurityHailerComponent comp, ClothingGotEquippedEvent args)
        {
            _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
        }

        private void OnMapInit(Entity<SecurityHailerComponent> ent, ref MapInitEvent args)
        {
            //COPY PASTED, IS THIS GOOD ?
            var (uid, comp) = ent;
            // test funny
            if (string.IsNullOrEmpty(comp.Action))
                return;

            _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
            Dirty(uid, comp);
        }
    }
}

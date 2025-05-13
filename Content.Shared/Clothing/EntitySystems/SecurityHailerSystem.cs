using Content.Shared.Actions;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems
{
    public sealed class SecurityHailerSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<ActionSecHailerActionEvent>(HailOrder);
        }

        private void HailOrder(ActionSecHailerActionEvent ev)
        {
            throw new NotImplementedException();
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

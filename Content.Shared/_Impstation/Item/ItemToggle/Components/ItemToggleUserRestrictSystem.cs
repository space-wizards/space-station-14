using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._Impstation.Item.ItemToggle.Components
{
    public sealed class ItemToggleUserRestrictSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemToggleUserRestrictComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
            SubscribeLocalEvent<ItemToggleUserRestrictComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);
        }

        private void OnActivateAttempt(Entity<ItemToggleUserRestrictComponent> ent, ref ItemToggleActivateAttemptEvent args)
        {
            if (!ent.Comp.OpenRestrict || args.Cancelled)
            {
                return;
            }

            foreach (var reg in ent.Comp.Components.Values)
            {
                var type = reg.Component.GetType();
                if (!HasComp(args.User, type))
                {
                    args.Cancelled = true;
                    if (ent.Comp.RestrictMessage != null)
                    {
                        args.Popup = Loc.GetString(ent.Comp.RestrictMessage);
                    }
                    break;
                }
            }
        }

        private void OnDeactivateAttempt(Entity<ItemToggleUserRestrictComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
        {
            if (!ent.Comp.CloseRestrict || args.Cancelled)
            {
                return;
            }

            foreach (var reg in ent.Comp.Components.Values)
            {
                var type = reg.Component.GetType();
                if (!HasComp(args.User, type))
                {
                    args.Cancelled = true;
                    //don't feel like adding popups to deactivateAttemptEvents so this stays
                    break;
                }
            }
        }
    }
}

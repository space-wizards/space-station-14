using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Shared.Item
{
    public class ItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedItemComponent, GetInteractionVerbsEvent>(AddPickupVerb);
        }

        private void AddPickupVerb(EntityUid uid, SharedItemComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                args.Using != null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !component.CanPickup(args.User, popup: false))
                return;

            Verb verb = new();
            verb.Act = () => args.Hands.TryPutInActiveHandOrAny(args.Target);
            verb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

            // if the item already in the user's inventory, change the text
            if (args.Target.TryGetContainer(out var container) && container.Owner == args.User)
                verb.Text = Loc.GetString("pick-up-verb-get-data-text-inventory");
            else
                verb.Text = Loc.GetString("pick-up-verb-get-data-text");

            args.Verbs.Add(verb);
        }
    }
}

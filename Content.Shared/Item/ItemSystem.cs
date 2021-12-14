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

            // if the item already in a container (that is not the same as the user's), then change the text.
            // this occurs when the item is in their inventory or in an open backpack
            args.User.TryGetContainer(out var userContainer);
            if (args.Target.TryGetContainer(out var container) && container != userContainer)
                verb.Text = Loc.GetString("pick-up-verb-get-data-text-inventory");
            else
                verb.Text = Loc.GetString("pick-up-verb-get-data-text");

            args.Verbs.Add(verb);
        }
    }
}

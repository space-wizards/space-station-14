using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;

namespace Content.Shared.Item
{
    public class ItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedItemComponent, GetVerbsEvent<InteractionVerb>>(AddPickupVerb);

            SubscribeLocalEvent<SharedSpriteComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<SharedSpriteComponent, GotUnequippedEvent>(OnUnequipped);

            SubscribeLocalEvent<SharedItemComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedItemComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, SharedItemComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ItemComponentState state)
                return;

            component.Size = state.Size;
            component.EquippedPrefix = state.EquippedPrefix;
            component.Color = state.Color;
            component.RsiPath = state.RsiPath;
        }

        private void OnGetState(EntityUid uid, SharedItemComponent component, ref ComponentGetState args)
        {
            args.State = new ItemComponentState(component.Size, component.EquippedPrefix, component.Color, component.RsiPath);
        }

        // Although netsync is being set to false for items client can still update these
        // Realistically:
        // Container should already hide these
        // Client is the only thing that matters.

        private void OnUnequipped(EntityUid uid, SharedSpriteComponent component, GotUnequippedEvent args)
        {
            component.Visible = true;
        }

        private void OnEquipped(EntityUid uid, SharedSpriteComponent component, GotEquippedEvent args)
        {
            component.Visible = false;
        }

        private void AddPickupVerb(EntityUid uid, SharedItemComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Hands == null ||
                args.Using != null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !args.Hands.CanPickupEntityToActiveHand(args.Target))
                return;

            InteractionVerb verb = new();
            verb.Act = () => args.Hands.TryPickupEntityToActiveHand(args.Target);
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

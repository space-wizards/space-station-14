using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using System;

namespace Content.Shared.Item
{
    public abstract class SharedItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedItemComponent, GetInteractionVerbsEvent>(AddPickupVerb);

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
        }

        private void OnGetState(EntityUid uid, SharedItemComponent component, ref ComponentGetState args)
        {
            args.State = new ItemComponentState(component.Size, component.EquippedPrefix);
        }

        private void OnUnequipped(EntityUid uid, SharedSpriteComponent component, GotUnequippedEvent args)
        {
            component.Visible = true;
        }

        private void OnEquipped(EntityUid uid, SharedSpriteComponent component, GotEquippedEvent args)
        {
            component.Visible = false;
        }

        private void AddPickupVerb(EntityUid uid, SharedItemComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null ||
                args.Using != null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !args.Hands.CanPickupEntityToActiveHand(args.Target))
                return;

            Verb verb = new();
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

        /// <summary>
        ///     Notifies any entity that is holding or wearing this item that they may need to update their sprite.
        /// </summary>
        public virtual void VisualsChanged(EntityUid owner, SharedItemComponent? item = null)
        { }
    }
}

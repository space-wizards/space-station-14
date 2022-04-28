using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Item
{
    public abstract class SharedItemSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedItemComponent, GetVerbsEvent<InteractionVerb>>(AddPickupVerb);

            SubscribeLocalEvent<SharedSpriteComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<SharedSpriteComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<SharedItemComponent, InteractHandEvent>(OnHandInteract);

            SubscribeLocalEvent<SharedItemComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedItemComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandInteract(EntityUid uid, SharedItemComponent component, InteractHandEvent args)
        {
            if (args.Handled || !component.CanPickup)
                return;

            args.Handled = _handsSystem.TryPickup(args.User, uid, animateUser: false);
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
                !component.CanPickup ||
                !_handsSystem.CanPickupAnyHand(args.User, args.Target, handsComp: args.Hands, item: component))
                return;

            InteractionVerb verb = new();
            verb.Act = () => _handsSystem.TryPickupAnyHand(args.User, args.Target, checkActionBlocker: false, handsComp: args.Hands, item: component);
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

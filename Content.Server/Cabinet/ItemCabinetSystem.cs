using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Cabinet;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Cabinet
{
    public class ItemCabinetSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemCabinetComponent, MapInitEvent>(OnMapInitialize);

            SubscribeLocalEvent<ItemCabinetComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ItemCabinetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ItemCabinetComponent, ActivateInWorldEvent>(OnActivateInWorld);

            SubscribeLocalEvent<ItemCabinetComponent, TryEjectItemCabinetEvent>(OnTryEjectItemCabinet);
            SubscribeLocalEvent<ItemCabinetComponent, TryInsertItemCabinetEvent>(OnTryInsertItemCabinet);
            SubscribeLocalEvent<ItemCabinetComponent, ToggleItemCabinetEvent>(OnToggleItemCabinet);

            SubscribeLocalEvent<ItemCabinetComponent, GetInteractionVerbsEvent>(AddEjectInsertVerbs);
            SubscribeLocalEvent<ItemCabinetComponent, GetActivationVerbsEvent>(AddToggleOpenVerb);
        }

        private void AddToggleOpenVerb(EntityUid uid, ItemCabinetComponent component, GetActivationVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // Toggle open verb
            Verb toggleVerb = new();
            toggleVerb.Act = () => OnToggleItemCabinet(uid, component);
            if (component.Opened)
            {
                toggleVerb.Text = Loc.GetString("verb-categories-close");
                toggleVerb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                toggleVerb.Text = Loc.GetString("verb-categories-open");
                toggleVerb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            args.Verbs.Add(toggleVerb);
        }

        private void AddEjectInsertVerbs(EntityUid uid, ItemCabinetComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // "Eject" item verb
            if (component.Opened &&
                component.ItemContainer.ContainedEntity != null &&
                _actionBlockerSystem.CanPickup(args.User))
            {
                Verb verb = new();
                verb.Act = () =>
                {
                    TakeItem(component, args.Hands, component.ItemContainer.ContainedEntity, args.User);
                    UpdateVisuals(component);
                };
                verb.Text = Loc.GetString("pick-up-verb-get-data-text");
                verb.IconTexture = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";
                args.Verbs.Add(verb);
            }

            // Insert item verb
            if (component.Opened &&
                args.Using != null &&
                _actionBlockerSystem.CanDrop(args.User) &&
                (component.Whitelist?.IsValid(args.Using) ?? true) &&
                component.ItemContainer.CanInsert(args.Using))
            {
                Verb verb = new();
                verb.Act = () =>
                {
                    args.Hands.TryPutEntityIntoContainer(args.Using, component.ItemContainer);
                    UpdateVisuals(component);
                };
                verb.Category = VerbCategory.Insert;
                verb.Text = args.Using.Name;
                args.Verbs.Add(verb);
            }
        }

        private void OnMapInitialize(EntityUid uid, ItemCabinetComponent comp, MapInitEvent args)
        {
            var owner = EntityManager.GetEntity(uid);
            comp.ItemContainer =
                owner.EnsureContainer<ContainerSlot>("item_cabinet", out _);

            if (comp.SpawnPrototype != null)
                comp.ItemContainer.Insert(EntityManager.SpawnEntity(comp.SpawnPrototype, owner.Transform.Coordinates));

            UpdateVisuals(comp);
        }

        private void OnInteractUsing(EntityUid uid, ItemCabinetComponent comp, InteractUsingEvent args)
        {
            args.Handled = true;
            if (!comp.Opened)
            {
                RaiseLocalEvent(uid, new ToggleItemCabinetEvent(), false);
            }
            else
            {
                RaiseLocalEvent(uid, new TryInsertItemCabinetEvent(args.User, args.Used), false);
            }

            args.Handled = true;
        }

        private void OnInteractHand(EntityUid uid, ItemCabinetComponent comp, InteractHandEvent args)
        {
            args.Handled = true;
            if (comp.Opened)
            {
                if (comp.ItemContainer.ContainedEntity == null)
                {
                    RaiseLocalEvent(uid, new ToggleItemCabinetEvent(), false);
                    return;
                }
                RaiseLocalEvent(uid, new TryEjectItemCabinetEvent(args.User), false);
            }
            else
            {
                RaiseLocalEvent(uid, new ToggleItemCabinetEvent(), false);
            }
        }

        private void OnActivateInWorld(EntityUid uid, ItemCabinetComponent comp, ActivateInWorldEvent args)
        {
            args.Handled = true;
            RaiseLocalEvent(uid, new ToggleItemCabinetEvent(), false);
        }

        /// <summary>
        ///     Toggles the ItemCabinet's state.
        /// </summary>
        private void OnToggleItemCabinet(EntityUid uid, ItemCabinetComponent comp, ToggleItemCabinetEvent? args = null)
        {
            comp.Opened = !comp.Opened;
            ClickLatchSound(comp);
            UpdateVisuals(comp);
        }

        /// <summary>
        ///     Tries to insert an entity into the ItemCabinet's slot from the user's hands.
        /// </summary>
        private static void OnTryInsertItemCabinet(EntityUid uid, ItemCabinetComponent comp, TryInsertItemCabinetEvent args)
        {
            if (comp.ItemContainer.ContainedEntity != null || args.Cancelled || (comp.Whitelist != null && !comp.Whitelist.IsValid(args.Item)))
            {
                return;
            }

            if (!args.User.TryGetComponent<HandsComponent>(out var hands) || !hands.Drop(args.Item, comp.ItemContainer))
            {
                return;
            }

            UpdateVisuals(comp);
        }

        /// <summary>
        ///     Tries to eject the ItemCabinet's item, either into the user's hands or onto the floor.
        /// </summary>
        private static void OnTryEjectItemCabinet(EntityUid uid, ItemCabinetComponent comp, TryEjectItemCabinetEvent args)
        {
            if (comp.ItemContainer.ContainedEntity == null || args.Cancelled)
                return;
            if (args.User.TryGetComponent(out SharedHandsComponent? hands))
            {
                // Put into hands
                TakeItem(comp, hands, comp.ItemContainer.ContainedEntity, args.User);
            }
            else if (comp.ItemContainer.Remove(comp.ItemContainer.ContainedEntity))
            {
                comp.ItemContainer.ContainedEntity.Transform.Coordinates = args.User.Transform.Coordinates;
            }
            UpdateVisuals(comp);
        }

        /// <summary>
        ///     Tries to eject the ItemCabinet's item, either into the user's hands. Used by both <see
        ///     cref="OnTryEjectItemCabinet"/> and the eject verbs.
        /// </summary>
        private static void TakeItem(ItemCabinetComponent comp, SharedHandsComponent hands, IEntity containedEntity, IEntity user)
        {
            if (containedEntity.HasComponent<ItemComponent>())
            {
                if (!hands.TryPutInActiveHandOrAny(containedEntity))
                    containedEntity.Transform.Coordinates = hands.Owner.Transform.Coordinates;

                comp.Owner.PopupMessage(user,
                    Loc.GetString("comp-item-cabinet-successfully-taken",
                        ("item", containedEntity),
                        ("cabinet", comp.Owner)));
            }
        }

        private static void UpdateVisuals(ItemCabinetComponent comp)
        {
            if (comp.Owner.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(ItemCabinetVisuals.IsOpen, comp.Opened);
                appearance.SetData(ItemCabinetVisuals.ContainsItem, comp.ItemContainer.ContainedEntity != null);
            }
        }

        private static void ClickLatchSound(ItemCabinetComponent comp)
        {
            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.DoorSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.15f));
        }
    }

    public class ToggleItemCabinetEvent : EntityEventArgs
    {
    }

    public class TryEjectItemCabinetEvent : CancellableEntityEventArgs
    {
        /// <summary>
        ///     The user who tried to eject the item.
        /// </summary>
        public IEntity User;

        public TryEjectItemCabinetEvent(IEntity user)
        {
            User = user;
        }
    }

    public class TryInsertItemCabinetEvent : CancellableEntityEventArgs
    {
        /// <summary>
        ///     The user who tried to eject the item.
        /// </summary>
        public IEntity User;

        /// <summary>
        ///     The item to be inserted.
        /// </summary>
        public IEntity Item;

        public TryInsertItemCabinetEvent(IEntity user, IEntity item)
        {
            User = user;
            Item = item;
        }
    }
}

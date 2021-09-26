using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Audio;
using Content.Shared.Cabinet;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Cabinet
{
    public class ItemCabinetSystem : EntitySystem
    {
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
        private void OnToggleItemCabinet(EntityUid uid, ItemCabinetComponent comp, ToggleItemCabinetEvent args)
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
            if (args.User.TryGetComponent(out HandsComponent? hands))
            {

                if (comp.ItemContainer.ContainedEntity.TryGetComponent<ItemComponent>(out var item))
                {
                    comp.Owner.PopupMessage(args.User,
                        Loc.GetString("comp-item-cabinet-successfully-taken",
                            ("item", comp.ItemContainer.ContainedEntity),
                            ("cabinet", comp.Owner)));
                    hands.PutInHandOrDrop(item);
                }
            }
            else if (comp.ItemContainer.Remove(comp.ItemContainer.ContainedEntity))
            {
                comp.ItemContainer.ContainedEntity.Transform.Coordinates = args.User.Transform.Coordinates;
            }
            UpdateVisuals(comp);
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

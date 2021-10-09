using Content.Shared.Audio;
using Content.Shared.Cabinet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using System;

namespace Content.Server.Cabinet
{
    public class ItemCabinetSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemCabinetComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ItemCabinetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ItemCabinetComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<ItemCabinetComponent, ComponentStartup>(InitializeAppearance);
            SubscribeLocalEvent<ItemCabinetComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
            SubscribeLocalEvent<ItemCabinetComponent, GetActivationVerbsEvent>(AddToggleOpenVerb);
        }

        private void InitializeAppearance(EntityUid uid, ItemCabinetComponent component, ComponentStartup args)
        {
            UpdateAppearance(uid, component);
        }

        private void UpdateAppearance(EntityUid uid,
            ItemCabinetComponent? cabinet = null,
            SharedItemSlotsComponent? itemSlots = null,
            SharedAppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref cabinet, ref itemSlots, ref appearance, false))
                return;

            appearance.SetData(ItemCabinetVisuals.IsOpen, cabinet.Opened);

            if (!itemSlots.Slots.TryGetValue(cabinet.CabinetSlot, out var slot))
                return;

            appearance.SetData(ItemCabinetVisuals.ContainsItem, slot.HasEntity);
        }

        private void OnItemSlotChanged(EntityUid uid, ItemCabinetComponent cabinet, ItemSlotChangedEvent args)
        {
            UpdateAppearance(uid, cabinet, args.SlotsComponent);
        }

        private void AddToggleOpenVerb(EntityUid uid, ItemCabinetComponent cabinet, GetActivationVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            // Toggle open verb
            Verb toggleVerb = new();
            toggleVerb.Act = () => ToggleItemCabinet(uid, cabinet);
            if (cabinet.Opened)
            {
                toggleVerb.Text = Loc.GetString("verb-common-close");
                toggleVerb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                toggleVerb.Text = Loc.GetString("verb-common-open");
                toggleVerb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            args.Verbs.Add(toggleVerb);
        }

        /// <summary>
        ///     Try insert an item if the cabinet is opened. Otherwise, just try open it.
        /// </summary>
        private void OnInteractUsing(EntityUid uid, ItemCabinetComponent comp, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!comp.Opened)
                ToggleItemCabinet(uid, comp);
            else
                _itemSlotsSystem.TryInsertContent(uid, args.Used, args.User);

            args.Handled = true;
        }

        /// <summary>
        ///     If the cabinet is opened and has an entity, try and take it. Otherwise toggle the cabinet open/closed;
        /// </summary>
        private void OnInteractHand(EntityUid uid, ItemCabinetComponent comp, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent(uid, out SharedItemSlotsComponent itemSlots))
                return;

            if (!itemSlots.Slots.TryGetValue(comp.CabinetSlot, out var slot))
                return;

            if (comp.Opened && slot.HasEntity)
                _itemSlotsSystem.TryEjectContent(uid, comp.CabinetSlot, args.User);
            else
                ToggleItemCabinet(uid, comp);

            args.Handled = true;
        }

        private void OnActivateInWorld(EntityUid uid, ItemCabinetComponent comp, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            ToggleItemCabinet(uid, comp);
        }

        /// <summary>
        ///     Toggles the ItemCabinet's state.
        /// </summary>
        private void ToggleItemCabinet(EntityUid uid, ItemCabinetComponent? cabinet = null)
        {
            if (!Resolve(uid, ref cabinet))
                return;

            cabinet.Opened = !cabinet.Opened;
            SoundSystem.Play(Filter.Pvs(uid), cabinet.DoorSound.GetSound(), uid, AudioHelpers.WithVariation(0.15f));

            UpdateAppearance(uid, cabinet);
        }
    }
}

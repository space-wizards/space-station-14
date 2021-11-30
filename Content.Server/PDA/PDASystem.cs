using Content.Server.Access.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.PDA
{
    public class PDASystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PDAComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<PDAComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<PDAComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<PDAComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
            SubscribeLocalEvent<PDAComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);

            SubscribeLocalEvent<PDAComponent, UplinkInitEvent>(OnUplinkInit);
            SubscribeLocalEvent<PDAComponent, UplinkRemovedEvent>(OnUplinkRemoved);
        }

        private void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            if (ui != null)
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);

            if (pda.IdCard != null)
                pda.IdSlot.StartingItem = pda.IdCard;
            _itemSlotsSystem.AddItemSlot(uid, $"{pda.Name}-id", pda.IdSlot);
            _itemSlotsSystem.AddItemSlot(uid, $"{pda.Name}-pen", pda.PenSlot);
        }

        private void OnComponentRemove(EntityUid uid, PDAComponent pda, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, pda.IdSlot);
            _itemSlotsSystem.RemoveItemSlot(uid, pda.PenSlot);
        }

        private void OnUse(EntityUid uid, PDAComponent pda, UseInHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = OpenUI(pda, args.User);
        }

        private void OnActivateInWorld(EntityUid uid, PDAComponent pda, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = OpenUI(pda, args.User);
        }

        private void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID == pda.IdSlot.ID)
                pda.ContainedID = args.Entity.GetComponentOrNull<IdCardComponent>();

            UpdatePDAAppearance(pda);
            UpdatePDAUserInterface(pda);
        }

        private void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == pda.IdSlot.ID)
                pda.ContainedID = null;

            UpdatePDAAppearance(pda);
            UpdatePDAUserInterface(pda);
        }

        private void OnLightToggle(EntityUid uid, PDAComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePDAUserInterface(pda);
        }

        public void SetOwner(PDAComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkInit(EntityUid uid, PDAComponent pda, UplinkInitEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkRemoved(EntityUid uid, PDAComponent pda, UplinkRemovedEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private bool OpenUI(PDAComponent pda, IEntity user)
        {
            if (!user.TryGetComponent(out ActorComponent? actor))
                return false;

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            ui?.Toggle(actor.PlayerSession);

            return true;
        }

        private void UpdatePDAAppearance(PDAComponent pda)
        {
            if (pda.Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(PDAVisuals.IDCardInserted, pda.ContainedID != null);
        }

        private void UpdatePDAUserInterface(PDAComponent pda)
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            var hasUplink = pda.Owner.HasComponent<UplinkComponent>();

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            ui?.SetState(new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, hasUplink));
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            // cast EntityUid? to EntityUid
            if (msg.Session.AttachedEntityUid is not EntityUid playerUid)
                return;

            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(pda);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (pda.Owner.TryGetComponent(out UnpoweredFlashlightComponent? flashlight))
                            _unpoweredFlashlight.ToggleLight(flashlight);
                        break;
                    }

                case PDAEjectIDMessage _:
                    {
                        _itemSlotsSystem.TryEjectToHands(pda.Owner.Uid, pda.IdSlot, playerUid);
                        break;
                    }
                case PDAEjectPenMessage _:
                    {
                        _itemSlotsSystem.TryEjectToHands(pda.Owner.Uid, pda.PenSlot, playerUid);
                        break;
                    }
                case PDAShowUplinkMessage _:
                    {
                        if (pda.Owner.TryGetComponent(out UplinkComponent? uplink))
                            _uplinkSystem.ToggleUplinkUI(uplink, msg.Session);
                        break;
                    }
            }
        }
    }
}

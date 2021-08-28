using Content.Server.Access.Components;
using Content.Server.Containers.ItemSlots;
using Content.Server.Light.Events;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;

namespace Content.Server.PDA
{
    public class PDASystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PDAComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PDAComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PDAComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<PDAComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<PDAComponent, ItemSlotChanged>(OnItemSlotChanged);
            SubscribeLocalEvent<PDAComponent, TrySetPDAOwner>(OnSetOwner);
        }

        private void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            if (ui != null)
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);

            UpdatePDAAppearance(pda);
        }

        private void OnMapInit(EntityUid uid, PDAComponent pda, MapInitEvent args)
        {
            // try to place ID inside item slot
            if (!string.IsNullOrEmpty(pda.StartingIdCard))
            {
                // if pda prototype doesn't have slots, ID will drop down on ground 
                var idCard = _entityManager.SpawnEntity(pda.StartingIdCard, pda.Owner.Transform.Coordinates);
                RaiseLocalEvent(uid, new PlaceItemAttempt(PDAComponent.IDSlotName, idCard));
            }
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

        private void OnItemSlotChanged(EntityUid uid, PDAComponent pda, ItemSlotChanged args)
        {
            // check if ID slot changed
            if (args.SlotName == PDAComponent.IDSlotName)
            {
                var item = args.Slot.ContainerSlot.ContainedEntity;
                if (item == null || !item.TryGetComponent(out IdCardComponent? idCard))
                    pda.ContainedID = null;
                else
                    pda.ContainedID = idCard;
            }
            else if (args.SlotName == PDAComponent.PenSlotName)
            {
                var item = args.Slot.ContainerSlot.ContainedEntity;
                pda.PenInserted = item != null;
            }

            UpdatePDAAppearance(pda);
            UpdatePDAUserInterface(pda);
        }

        private void OnSetOwner(EntityUid uid, PDAComponent pda, TrySetPDAOwner args)
        {
            pda.OwnerName = args.OwnerName;
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

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            ui?.SetState(new PDAUpdateState(false, pda.PenInserted, ownerInfo));
        }

        private void OnUIMessage(PDAComponent component, ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(component);
                    break;
                case PDAToggleFlashlightMessage _:
                    RaiseLocalEvent(component.Owner.Uid, new TryToggleLightEvent());
                    break;
                case PDAEjectIDMessage _:
                    {
                        var ejectAttempt = new EjectItemAttempt(PDAComponent.IDSlotName, msg.Session.AttachedEntity);
                        RaiseLocalEvent(component.Owner.Uid, ejectAttempt);
                        break;
                    }
                case PDAEjectPenMessage _:
                    {
                        var ejectAttempt = new EjectItemAttempt(PDAComponent.PenSlotName, msg.Session.AttachedEntity);
                        RaiseLocalEvent(component.Owner.Uid, ejectAttempt);
                        break;
                    }
            }
        }
    }
}

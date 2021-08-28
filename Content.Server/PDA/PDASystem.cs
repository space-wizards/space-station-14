using Content.Server.Access.Components;
using Content.Server.Containers.ItemSlots;
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
            if (pda.UserInterface != null)
                pda.UserInterface.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);

            UpdatePDAAppearance(pda);
        }

        private void OnMapInit(EntityUid uid, PDAComponent pda, MapInitEvent args)
        {
            if (!string.IsNullOrEmpty(pda.StartingIdCard))
            {
                // if pda prototype doesn't have slot, ID will drop down on ground 
                var idCard = _entityManager.SpawnEntity(pda.StartingIdCard, pda.Owner.Transform.Coordinates);
                RaiseLocalEvent(uid, new PlaceItemAttempt(PDAComponent.IDSlotName, idCard));
            }
        }

        private void OnUse(EntityUid uid, PDAComponent pda, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            pda.UserInterface?.Toggle(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnActivateInWorld(EntityUid uid, PDAComponent pda, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            pda.UserInterface?.Toggle(actor.PlayerSession);
            args.Handled = true;
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

                UpdatePDAAppearance(pda);
                UpdatePDAUserInterface(pda);
            }
        }

        private void UpdatePDAAppearance(PDAComponent pda)
        {
            if (pda.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                //appearance.SetData(PDAVisuals.FlashlightLit, _lightOn);
                appearance.SetData(PDAVisuals.IDCardInserted, pda.ContainedID != null);
            }
        }

        private void UpdatePDAUserInterface(PDAComponent pda)
        {
            /*var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = OwnerName,
                IdOwner = ContainedID?.FullName,
                JobTitle = ContainedID?.JobTitle
            };

            //Do we have an account? If so provide the info.
            if (_syndicateUplinkAccount != null)
            {
                var accData = new UplinkAccountData(_syndicateUplinkAccount.AccountHolder,
                    _syndicateUplinkAccount.Balance);
                var listings = _uplinkManager.FetchListings.Values.ToArray();
                // TODO: fix pen slot
                UserInterface?.SetState(new PDAUpdateState(_lightOn, false, ownerInfo, accData, listings));
            }
            else
            {
                // TODO: fix pen slot
                UserInterface?.SetState(new PDAUpdateState(_lightOn, false, ownerInfo));
            }*/
        }

        private void OnUIMessage(PDAComponent component, ServerBoundUserInterfaceMessage msg)
        {
            /*switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    {
                        UpdatePDAUserInterface(component);
                        break;
                    }
                case PDAToggleFlashlightMessage _:
                    {
                        ToggleLight();
                        break;
                    }

                case PDAEjectIDMessage _:
                    {
                        // TODO: fix id slot
                        //HandleIDEjection(message.Session.AttachedEntity!);
                        break;
                    }

                case PDAEjectPenMessage _:
                    {
                        // TODO: fix pen slot
                        //HandlePenEjection(message.Session.AttachedEntity!);
                        break;
                    }

                case PDAUplinkBuyListingMessage buyMsg:
                    {
                        var player = message.Session.AttachedEntity;
                        if (player == null) break;

                        if (!_uplinkManager.TryPurchaseItem(_syndicateUplinkAccount, buyMsg.ItemId,
                            player.Transform.Coordinates, out var entity))
                        {
                            SendNetworkMessage(new PDAUplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                            break;
                        }

                        if (!player.TryGetComponent(out HandsComponent? hands) ||
                            !entity.TryGetComponent(out ItemComponent? item))
                            break;

                        hands.PutInHandOrDrop(item);

                        SendNetworkMessage(new PDAUplinkBuySuccessMessage(), message.Session.ConnectedClient);
                        break;
                    }
            }*/
        }

        private void OnSetOwner(EntityUid uid, PDAComponent pda, TrySetPDAOwner args)
        {
            pda.OwnerName = args.OwnerName;
            UpdatePDAUserInterface(pda);
        }
    }
}

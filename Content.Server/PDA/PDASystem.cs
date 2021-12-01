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
    public sealed class PDASystem : SharedPDASystem
    {
        [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, ComponentInit>(OnComponentInit);

            SubscribeLocalEvent<PDAComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<PDAComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
        }

        protected override void OnComponentInit(EntityUid uid, SharedPDAComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            if (ui != null)
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);

            if (pda.IdCard != null)
                pda.IdSlot.StartingItem = pda.IdCard;
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

        protected override void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePDAUserInterface(pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
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
                        ItemSlotsSystem.TryEjectToHands(pda.Owner.Uid, pda.IdSlot, playerUid);
                        break;
                    }
                case PDAEjectPenMessage _:
                    {
                        ItemSlotsSystem.TryEjectToHands(pda.Owner.Uid, pda.PenSlot, playerUid);
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

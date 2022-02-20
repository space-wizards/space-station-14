using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.PDA.Ringer;
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
        [Dependency] private readonly UplinkAccountsSystem _uplinkAccounts = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly RingerSystem _ringerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
        }

        protected override void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            if (ui != null)
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);
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

        private bool OpenUI(PDAComponent pda, EntityUid user)
        {
            if (!EntityManager.TryGetComponent(user, out ActorComponent? actor))
                return false;

            UpdatePDAUserInterface(pda, user);
            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            ui?.Toggle(actor.PlayerSession);

            return true;
        }

        private void UpdatePDAAppearance(PDAComponent pda)
        {
            if (EntityManager.TryGetComponent(pda.Owner, out AppearanceComponent? appearance))
                appearance.SetData(PDAVisuals.IDCardInserted, pda.ContainedID != null);
        }

        private void UpdatePDAUserInterface(PDAComponent pda, EntityUid? user = null)
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            var ui = pda.Owner.GetUIOrNull(PDAUiKey.Key);
            if (ui == null)
                return;

            ui.SetState(new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, ShouldShowUplink(pda.Owner, ui, user)));
        }

        /// <summary>
        ///     Check whether the PDA has an uplink, and ensure that the only person that can see the PDA UI has an
        ///     uplink account.
        /// </summary>
        public bool ShouldShowUplink(EntityUid uid, BoundUserInterface ui, EntityUid? user = null)
        {
            // TODO UPLINK RINGTONES/SECRETS This is just a janky placeholder way of hiding uplinks from non syndicate
            // players. This should really use a sort of key-code entry system that selects an account which is not directly tied to
            // a player entity.

            if (!HasComp<UplinkComponent>(uid))
                return false;

            // If a user is trying to open the UI, make sure that they have an uplink account before showing the UI.
            if (user != null && !_uplinkAccounts.HasAccount(user.Value))
                return false;

            // If other users currently have the UI open, check that they too should be allowed to see the button..
            foreach (var session in ui.SubscribedSessions)
            {
                if (session.AttachedEntity != null && !_uplinkAccounts.HasAccount(session.AttachedEntity.Value))
                    return false;
            }

            // everyone has an uplink account, show the button.
            return true;
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            // cast EntityUid? to EntityUid
            if (msg.Session.AttachedEntity is not {Valid: true} playerUid)
                return;

            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(pda, playerUid);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out UnpoweredFlashlightComponent? flashlight))
                            _unpoweredFlashlight.ToggleLight(flashlight);
                        break;
                    }

                case PDAEjectIDMessage _:
                    {
                        ItemSlotsSystem.TryEjectToHands(pda.Owner, pda.IdSlot, playerUid);
                        break;
                    }
                case PDAEjectPenMessage _:
                    {
                        ItemSlotsSystem.TryEjectToHands(pda.Owner, pda.PenSlot, playerUid);
                        break;
                    }
                case PDAShowUplinkMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out UplinkComponent? uplink))
                            _uplinkSystem.ToggleUplinkUI(uplink, msg.Session);
                        break;
                    }
                case PDAShowRingtoneMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out RingerComponent? ringer))
                            _ringerSystem.ToggleRingerUI(ringer, msg.Session);
                        break;
                    }
            }
        }
    }
}

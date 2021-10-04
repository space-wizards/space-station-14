using Content.Server.Access.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.Traitor.Uplink.Systems;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.PDA
{
    public class PDASystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedItemSlotsSystem _slotsSystem = default!;
        [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PDAComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PDAComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<PDAComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<PDAComponent, ItemSlotChanged>(OnItemSlotChanged);
            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);

            SubscribeLocalEvent<PDAComponent, UplinkInitEvent>(OnUplinkInit);
            SubscribeLocalEvent<PDAComponent, UplinkRemovedEvent>(OnUplinkRemoved);

            SubscribeLocalEvent<PDAComponent, GetAlternativeVerbsEvent>(AddEjectVerb);
            SubscribeLocalEvent<PDAComponent, GetInteractionVerbsEvent>(AddInsertVerb);
            SubscribeLocalEvent<PDAComponent, GetActivationVerbsEvent>(AddToggleLightVerb);
        }

        private void AddToggleLightVerb(EntityUid uid, PDAComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("verb-toggle-light");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = () => component.ToggleLight();
            verb.Priority = -1; // whenever Open-UI verb is added, that should be higher priority.
            args.Verbs.Add(verb);
        }

        // TODO VERBS EJECTABLES Standardize eject/insert verbs into a single system?
        private void AddEjectVerb(EntityUid uid, PDAComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanPickup(args.User))
                return;

            // eject ID
            if (!component.IdSlotEmpty)
            {
                Verb verb = new();
                verb.Text = component.IdSlot.ContainedEntity!.Name;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => component.HandleIDEjection(args.User);
                args.Verbs.Add(verb);
            }

            // eject pen
            if (!component.PenSlotEmpty)
            {
                Verb verb = new();
                verb.Text = component.PenSlot.ContainedEntity!.Name;
                verb.Category = VerbCategory.Eject;
                verb.Act = () => component.HandlePenEjection(args.User);
                verb.Priority = -1; // ID takes priority.
                args.Verbs.Add(verb);
            }
        }

        private void AddInsertVerb(EntityUid uid, PDAComponent component, GetInteractionVerbsEvent args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                !_actionBlockerSystem.CanDrop(args.User))
                return;

            // insert ID
            if (component.IdSlotEmpty &&
                args.Using.TryGetComponent(out IdCardComponent? id))
            {
                Verb verb = new();
                verb.Text = args.Using.Name;
                verb.Category = VerbCategory.Insert;
                verb.Act = () =>
                {
                    component.InsertIdCard(id);
                    component.UpdatePDAUserInterface();
                };
                args.Verbs.Add(verb);
            }

            // insert pen
            if (component.PenSlotEmpty &&
                args.Using.HasTag("Write"))
            {
                Verb verb = new();
                verb.Text = args.Using.Name;
                verb.Category = VerbCategory.Insert;
                verb.Act = () =>
                {
                    component.PenSlot.Insert(args.Using);
                    component.UpdatePDAUserInterface();
                };
                args.Verbs.Add(verb);
            }
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
                var idCard = EntityManager.SpawnEntity(pda.StartingIdCard, pda.Owner.Transform.Coordinates);
                if (EntityManager.TryGetComponent(uid, out SharedItemSlotsComponent? itemSlots))
                    _slotsSystem.TryInsertContent(itemSlots, idCard, PDAComponent.IDSlotName);
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
                var item = args.ContainedItem;
                if (item == null || !EntityManager.TryGetComponent(item.Value, out IdCardComponent ? idCard))
                    pda.ContainedID = null;
                else
                    pda.ContainedID = idCard;
            }
            else if (args.SlotName == PDAComponent.PenSlotName)
            {
                var item = args.ContainedItem;
                pda.PenInserted = item != null;
            }

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
            ui?.SetState(new PDAUpdateState(pda.FlashlightOn, pda.PenInserted, ownerInfo, hasUplink));
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
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
                        if (pda.Owner.TryGetComponent(out SharedItemSlotsComponent? itemSlots))
                            _slotsSystem.TryEjectContent(itemSlots, PDAComponent.IDSlotName, msg.Session.AttachedEntity);
                        break;
                    }
                case PDAEjectPenMessage _:
                    {
                        if (pda.Owner.TryGetComponent(out SharedItemSlotsComponent? itemSlots))
                            _slotsSystem.TryEjectContent(itemSlots, PDAComponent.PenSlotName, msg.Session.AttachedEntity);
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

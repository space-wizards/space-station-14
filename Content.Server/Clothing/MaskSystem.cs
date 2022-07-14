using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Clothing.Components;
using Content.Server.Disease.Components;
using Content.Server.IdentityManagement;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Player;

namespace Content.Server.Clothing
{
    public sealed class MaskSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ActionsSystem _actionSystem = default!;
        [Dependency] private readonly IdentitySystem _identity = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MaskComponent, ToggleMaskEvent>(OnToggleMask);
            SubscribeLocalEvent<MaskComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<MaskComponent, GotUnequippedEvent>(OnGotUnequipped);
        }

        private void OnGetActions(EntityUid uid, MaskComponent component, GetItemActionsEvent args)
        {
            if (component.ToggleAction != null && !args.InHands)
                args.Actions.Add(component.ToggleAction);
        }

        private void OnToggleMask(EntityUid uid, MaskComponent mask, ToggleMaskEvent args)
        {
            if (mask.ToggleAction == null)
                return;

            if (!_inventorySystem.TryGetSlotEntity(args.Performer, "mask", out var existing) || !mask.Owner.Equals(existing))
                return;

            mask.IsToggled ^= true;
            _actionSystem.SetToggled(mask.ToggleAction, mask.IsToggled);

            // Pulling mask down can change identity, so we want to update that
            _identity.QueueIdentityUpdate(args.Performer);

            if (mask.IsToggled)
                _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-down-popup-message", ("mask", mask.Owner)), args.Performer, Filter.Entities(args.Performer));
            else
                _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-up-popup-message", ("mask", mask.Owner)), args.Performer, Filter.Entities(args.Performer));

            ToggleMaskComponents(uid, mask, args.Performer);
        }

        // set to untoggled when unequipped, so it isn't left in a 'pulled down' state
        private void OnGotUnequipped(EntityUid uid, MaskComponent mask, GotUnequippedEvent args)
        {
            if (mask.ToggleAction == null)
                return;

            mask.IsToggled = false;
            _actionSystem.SetToggled(mask.ToggleAction, mask.IsToggled);

            ToggleMaskComponents(uid, mask, args.Equipee, true);
        }

        private void ToggleMaskComponents(EntityUid uid, MaskComponent mask, EntityUid wearer, bool isEquip = false)
        {
            //toggle visuals
            if (TryComp<SharedItemComponent>(mask.Owner, out var item))
            {
                //TODO: sprites for 'pulled down' state. defaults to invisible due to no sprite with this prefix
                item.EquippedPrefix = mask.IsToggled ? "toggled" : null;
                Dirty(item);
            }

            // toggle ingestion blocking
            if (TryComp<IngestionBlockerComponent>(uid, out var blocker))
                blocker.Enabled = !mask.IsToggled;

            // toggle disease protection
            if (TryComp<DiseaseProtectionComponent>(uid, out var diseaseProtection))
                diseaseProtection.IsActive = !mask.IsToggled;

            // toggle identity
            if (TryComp<IdentityBlockerComponent>(uid, out var identity))
                identity.Enabled = !mask.IsToggled;

            // toggle breath tool connection (skip during equip since that is handled in LungSystem)
            if (isEquip || !TryComp<BreathToolComponent>(uid, out var breathTool))
                return;

            if (mask.IsToggled)
            {
                breathTool.DisconnectInternals();
            }
            else
            {
                breathTool.IsFunctional = true;

                if (TryComp(wearer, out InternalsComponent? internals))
                {
                    breathTool.ConnectedInternalsEntity = wearer;
                    internals.ConnectBreathTool(uid);
                }
            }
        }
    }
}

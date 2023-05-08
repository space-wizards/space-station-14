using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Clothing.Components;
using Content.Server.IdentityManagement;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.VoiceMask;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server.Clothing
{
    public sealed class MaskSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actionSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly InternalsSystem _internals = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IdentitySystem _identity = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;

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
                _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-down-popup-message", ("mask", mask.Owner)), args.Performer, args.Performer);
            else
                _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-up-popup-message", ("mask", mask.Owner)), args.Performer, args.Performer);

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
            // toggle visuals
            if (TryComp<ClothingComponent>(uid, out var clothing))
            {
                //TODO: sprites for 'pulled down' state. defaults to invisible due to no sprite with this prefix
                _clothing.SetEquippedPrefix(uid, mask.IsToggled ? "toggled" : null, clothing);
            }

            // shouldn't this be an event?

            // toggle ingestion blocking
            if (TryComp<IngestionBlockerComponent>(uid, out var blocker))
                blocker.Enabled = !mask.IsToggled;

            // toggle identity
            if (TryComp<IdentityBlockerComponent>(uid, out var identity))
                identity.Enabled = !mask.IsToggled;

            // toggle voice masking
            if (TryComp<VoiceMaskComponent>(wearer, out var voiceMask))
                voiceMask.Enabled = !mask.IsToggled;

            // toggle breath tool connection (skip during equip since that is handled in LungSystem)
            if (isEquip || !TryComp<BreathToolComponent>(uid, out var breathTool))
                return;

            if (mask.IsToggled)
            {
                _atmos.DisconnectInternals(breathTool);
            }
            else
            {
                breathTool.IsFunctional = true;

                if (TryComp(wearer, out InternalsComponent? internals))
                {
                    breathTool.ConnectedInternalsEntity = wearer;
                    _internals.ConnectBreathTool(internals, uid);
                }
            }
        }
    }
}

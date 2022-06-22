using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Toggleable;
using Content.Shared.Inventory;
using Content.Server.Body.Components;
using Content.Server.Clothing.Components;
using Content.Server.Disease.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Clothing.EntitySystems
{
    public sealed class ToggleableClothingSystem : SharedToggleableClothingSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ToggleableClothingComponent, ToggleClothingEvent>(OnToggleClothing);
            SubscribeLocalEvent<ToggleableClothingComponent, GetItemActionsEvent>(OnGetActions);
        }

        private void OnGetActions(EntityUid uid, ToggleableClothingComponent component, GetItemActionsEvent args)
        {
            if (component.ToggleAction != null && component.SelfToggle)
            {
                // logic for self toggled clothing like masks
                args.Actions.Add(component.ToggleAction);
                ResetSelfToggleClothing(uid, component);
            }
            else
            {
                // logic for toggleable clothing tied to another like hardsuits
                if (component.ClothingUid == null || (args.SlotFlags & component.RequiredFlags) != component.RequiredFlags)
                    return;

                if (component.ToggleAction != null)
                    args.Actions.Add(component.ToggleAction);
            }
        }

        private void OnToggleClothing(EntityUid uid, ToggleableClothingComponent component, ToggleClothingEvent args)
        {
            // self toggled clothing, like masks
            if (component.SelfToggle)
            {
                OnSelfToggleClothing(uid, component, args);
                args.Handled = true;
                return;
            }

            // continue on for standard toggleable clothing, like hardsuits
            if (args.Handled || component.Container == null || component.ClothingUid == null)
                return;

            var parent = Transform(uid).ParentUid;
            if (component.Container.ContainedEntity == null)
                _inventorySystem.TryUnequip(parent, component.Slot);
            else if (_inventorySystem.TryGetSlotEntity(parent, component.Slot, out var existing))
            {
                _popupSystem.PopupEntity(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                    args.Performer, Filter.Entities(args.Performer));
            }
            else
                _inventorySystem.TryEquip(parent, component.ClothingUid.Value, component.Slot);

            args.Handled = true;
        }

        private void OnSelfToggleClothing(EntityUid uid, ToggleableClothingComponent component, ToggleClothingEvent args)
        {
            if (args.Handled)
                return;

            switch (component.Slot)
            {
                case "mask":
                    if (TryEquip(uid, component, args, component.Slot))
                    {
                        component.IsToggled ^= true;
                        ToggleMask(uid, component, args.Performer);
                        //TODO: update visuals?
                    }
                    break;
            }
        }

        // reset on pickup to ensure not initially equipped in toggled state
        private void ResetSelfToggleClothing(EntityUid uid, ToggleableClothingComponent component)
        {
            switch (component.Slot)
            {
                case "mask":
                    component.IsToggled = false;
                    ToggleMask(uid, component, default, false);
                    break;
            }
        }

        // On toggle, try to equip if not already equipped
        private bool TryEquip(EntityUid uid, ToggleableClothingComponent component, ToggleClothingEvent args, string slot)
        {
            if (_inventorySystem.TryGetSlotEntity(args.Performer, slot, out var existing) && !component.Owner.Equals(existing))
            {
                _popupSystem.PopupEntity(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                                    args.Performer, Filter.Entities(args.Performer));
                return false;
            }
            else if (existing == null)
            {
                _inventorySystem.TryEquip(args.Performer, component.Owner, slot);
                return false;
            }

            return true;
        }

        // unique behavior for MASK slot
        private void ToggleMask(EntityUid uid, ToggleableClothingComponent component, EntityUid wearer, bool message = true)
        {
            if (message && wearer != default)
            {
                if (!component.IsToggled)
                {
                    _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-up-popup-message", ("mask", component.Owner)), wearer, Filter.Entities(wearer));
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("action-mask-pull-down-popup-message", ("mask", component.Owner)), wearer, Filter.Entities(wearer));
                }
            }

            EntitySystem.Get<SharedActionsSystem>().SetToggled(component.ToggleAction!, component.IsToggled);

            //toggle ingestion blocking
            if (TryComp<IngestionBlockerComponent>(uid, out var blocker))
            {
                blocker.Enabled = !component.IsToggled;
            }

            //toggle disease protection
            if (TryComp<DiseaseProtectionComponent>(uid, out var diseaseProtection))
            {
                diseaseProtection.IsActive = !component.IsToggled;
            }

            //toggle breath tool conneciton
            if (TryComp<BreathToolComponent>(uid, out var breathTool))
            {
                if (wearer != default) //connected to internals, so skip if just resetting
                {
                    if (component.IsToggled)
                        breathTool.DisconnectInternals();
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
    }
}

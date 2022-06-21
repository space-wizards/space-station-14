using System.Threading.Tasks;
using Content.Server.Clothing.Components;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Shared.Inventory;
using Content.Server.Body.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Server.Disease.Components;

namespace Content.Server.Clothing
{
    public sealed class ClothingSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ClothingComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<ClothingComponent, ToggleActionEvent>(OnActionToggle);
        }

        private void OnGetActions(EntityUid uid, ClothingComponent component, GetItemActionsEvent args)
        {
            if (component.ToggleAction != null)
            {
                component.IsToggled = true;
                args.Actions.Add(component.ToggleAction);
                //reset on pickup to ensure not equipped in untoggled state
                ToggleMask(uid, component, default, false);
            }
        }

        private void OnActionToggle(EntityUid uid, ClothingComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if ((component.SlotFlags & SlotFlags.MASK) != 0)
            {
                if (TryEquip(uid, component, args, "mask"))
                {
                    component.IsToggled ^= true;
                    ToggleMask(uid, component, args.Performer);
                }
            }
        }

        // On toggle, try to equip if not already equipped
        private bool TryEquip(EntityUid uid, ClothingComponent component, ToggleActionEvent args, string slot)
        {
            if (_inventorySystem.TryGetSlotEntity(args.Performer, slot, out var existing) && !component.Owner.Equals(existing))
            {
                _popupSystem.PopupEntity(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                                    args.Performer, Filter.Entities(args.Performer));
                return false;
            }
            else if(existing == null)
            {
                _inventorySystem.TryEquip(args.Performer, component.Owner, slot);
                return false;
            }

            return true;
        }

        // unique behavior for MASK slot
        private void ToggleMask(EntityUid uid, ClothingComponent component, EntityUid wearer, bool message = true)
        {
            if (message && wearer != default)
            {
                if (component.IsToggled)
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
                blocker.Enabled = component.IsToggled;
            }

            //toggle disease protection
            if (TryComp<DiseaseProtectionComponent>(uid, out var diseaseProtection))
            {
                diseaseProtection.IsActive = component.IsToggled;
            }

            //toggle breath tool conneciton
            if (TryComp<BreathToolComponent>(uid, out var breathTool))
            {
                if (!component.IsToggled)
                    breathTool.DisconnectInternals();
                else if(wearer != default)
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

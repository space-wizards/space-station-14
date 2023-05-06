using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.VendingMachines.Restock
{
    public sealed class VendingMachineRestockSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly PricingSystem _pricingSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
        }

        public bool TryAccessMachine(EntityUid uid,
            VendingMachineRestockComponent restock,
            VendingMachineComponent machineComponent,
            EntityUid user,
            EntityUid target)
        {
            if (!TryComp<WiresPanelComponent>(target, out var panel) || !panel.Open)
            {
                _popupSystem.PopupCursor(Loc.GetString("vending-machine-restock-needs-panel-open",
                        ("this", uid),
                        ("user", user),
                        ("target", target)),
                    user);
                return false;
            }

            return true;
        }

        public bool TryMatchPackageToMachine(EntityUid uid,
            VendingMachineRestockComponent component,
            VendingMachineComponent machineComponent,
            EntityUid user,
            EntityUid target)
        {
            if (!component.CanRestock.Contains(machineComponent.PackPrototypeId))
            {
                _popupSystem.PopupCursor(Loc.GetString("vending-machine-restock-invalid-inventory", ("this", uid), ("user", user), ("target", target)), user);
                return false;
            }

            return true;
        }

        private void OnAfterInteract(EntityUid uid, VendingMachineRestockComponent component, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || args.Handled)
                return;

            if (!TryComp<VendingMachineComponent>(args.Target, out var machineComponent))
                return;

            if (!TryMatchPackageToMachine(uid, component, machineComponent, args.User, args.Target.Value))
                return;

            if (!TryAccessMachine(uid, component, machineComponent, args.User, args.Target.Value))
                return;

            args.Handled = true;

            var doAfterArgs = new DoAfterArgs(args.User, (float) component.RestockDelay.TotalSeconds, new RestockDoAfterEvent(), args.Target,
                target: args.Target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

            if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
                return;

            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-start", ("this", uid), ("user", args.User), ("target", args.Target)),
                args.User,
                PopupType.Medium);

            _audioSystem.PlayPvs(component.SoundRestockStart, component.Owner, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));
        }

        private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
        {
            List<double> priceSets = new();

            // Find the most expensive inventory and use that as the highest price.
            foreach (var vendingInventory in component.CanRestock)
            {
                double total = 0;

                if (_prototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
                {
                    foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                    {
                        if (_prototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricingSystem.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            args.Price += priceSets.Max();
        }
    }
}

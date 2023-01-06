using System.Linq;
using System.Threading;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Server.Cargo.Systems;
using Content.Server.DoAfter;
using Content.Server.Wires;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines;

namespace Content.Server.VendingMachines.Restock
{
    public sealed class VendingMachineRestockSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly PricingSystem _pricingSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
            SubscribeLocalEvent<VendingMachineRestockComponent, RestockCancelledEvent>(OnRestockCancelled);
        }

        public bool TryAccessMachine(EntityUid uid,
            VendingMachineRestockComponent component,
            VendingMachineComponent machineComponent,
            EntityUid user,
            EntityUid target)
        {
            if (!TryComp<WiresComponent>(target, out var wires) || !wires.IsPanelOpen) {
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
            if (!component.CanRestock.Contains(machineComponent.PackPrototypeId)) {
                _popupSystem.PopupCursor(Loc.GetString("vending-machine-restock-invalid-inventory",
                        ("this", uid),
                        ("user", user),
                        ("target", target)
                        ),
                    user);
                return false;
            }

            return true;
        }

        private void OnAfterInteract(EntityUid uid, VendingMachineRestockComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
                return;

            if (!TryComp<VendingMachineComponent>(args.Target, out var machineComponent))
                return;

            if (!TryMatchPackageToMachine(uid, component, machineComponent, args.User, args.Target.Value))
                return;

            if (!TryAccessMachine(uid, component, machineComponent, args.User, args.Target.Value))
                return;

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(
                    args.User,
                    (float) component.RestockDelay.TotalSeconds,
                    component.CancelToken.Token,
                    args.Target,
                    args.Used)
            {
                TargetFinishedEvent = new VendingMachineRestockEvent(args.User, uid),
                UsedCancelledEvent = new RestockCancelledEvent(),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
                NeedHand = true
            });

            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-start",
                    ("this", uid),
                    ("user", args.User),
                    ("target", args.Target)
                    ),
                args.User,
                PopupType.Medium);

            _audioSystem.PlayPvs(component.SoundRestockStart, component.Owner,
                AudioParams.Default
                .WithVolume(-2f)
                .WithVariation(0.2f));
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

        private void OnRestockCancelled(EntityUid uid, VendingMachineRestockComponent component, RestockCancelledEvent args)
        {
            component.CancelToken?.Cancel();
            component.CancelToken = null;
        }

        public readonly struct RestockCancelledEvent { }
    }
}

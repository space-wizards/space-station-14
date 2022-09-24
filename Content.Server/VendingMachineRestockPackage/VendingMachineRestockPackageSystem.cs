using System.Threading;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Server.DoAfter;
using Content.Server.VendingMachines;
using Content.Server.Wires;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines;

namespace Content.Server.VendingMachineRestockPackage
{
    public sealed class VendingMachineRestockPackageSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineRestockPackageComponent, AfterInteractEvent>(OnAfterInteract);

            SubscribeLocalEvent<RestockSuccessfulEvent>(OnRestockSuccessful);
            SubscribeLocalEvent<RestockCancelledEvent>(OnRestockCancelled);
        }

        public bool TryAccessMachine(EntityUid uid,
            VendingMachineRestockPackageComponent component,
            VendingMachineComponent machineComponent,
            EntityUid user,
            EntityUid target)
        {
            if (!TryComp<WiresComponent>(target, out var wires) || !wires.IsPanelOpen) {
                _popupSystem.PopupCursor(Loc.GetString("vending-machine-restock-package-needs-panel-open",
                        ("this", uid),
                        ("user", user),
                        ("target", target)
                        ),
                    Filter.Entities(user));
                return false;
            }

            return true;
        }

        public bool TryMatchPackageToMachine(EntityUid uid,
            VendingMachineRestockPackageComponent component,
            VendingMachineComponent machineComponent,
            EntityUid user,
            EntityUid target)
        {
            if (!component.CanRestock.Contains(machineComponent.PackPrototypeId)) {
                _popupSystem.PopupCursor(Loc.GetString("vending-machine-restock-package-invalid-inventory",
                        ("this", uid),
                        ("user", user),
                        ("target", target)
                        ),
                    Filter.Entities(user));
                return false;
            }

            return true;
        }

        private void OnAfterInteract(EntityUid uid, VendingMachineRestockPackageComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
                return;

            if (!TryComp<VendingMachineComponent>(args.Target, out var machineComponent))
                return;

            if (!TryMatchPackageToMachine(uid, component, machineComponent, args.User, args.Target.GetValueOrDefault()))
                return;

            if (!TryAccessMachine(uid, component, machineComponent, args.User, args.Target.GetValueOrDefault()))
                return;

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.RestockDelay, component.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new RestockSuccessfulEvent(args.User, (EntityUid) args.Target, component.Owner),
                BroadcastCancelledEvent = new RestockCancelledEvent(component.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });

            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-package-start",
                    ("this", uid),
                    ("user", args.User),
                    ("target", args.Target)
                    ),
                args.User,
                Filter.Pvs(args.User),
                PopupType.Medium);

            _audioSystem.Play(component.SoundRestockStart, Filter.Pvs(component.Owner), component.Owner, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));
        }

        private void OnRestockCancelled(RestockCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Package, out VendingMachineRestockPackageComponent? package))
                return;
            package.CancelToken = null;
        }

        private void OnRestockSuccessful(RestockSuccessfulEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Package, out VendingMachineRestockPackageComponent? package))
                return;

            package.CancelToken = null;

            RaiseLocalEvent(ev.Target, new VendingMachineRestockEvent(), false);

            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-package-done",
                    ("this", ev.Package),
                    ("user", ev.User),
                    ("target", ev.Target)
                    ),
                ev.User,
                Filter.Pvs(ev.User),
                PopupType.Medium);

            _audioSystem.Play(package.SoundRestockDone, Filter.Pvs(package.Owner), package.Owner, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

            QueueDel(ev.Package);
        }

        private sealed class RestockCancelledEvent : EntityEventArgs
        {
            public EntityUid Package;

            public RestockCancelledEvent(EntityUid package)
            {
                Package = package;
            }
        }

        private sealed class RestockSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public EntityUid Package;
            public RestockSuccessfulEvent(EntityUid user, EntityUid target, EntityUid package)
            {
                User = user;
                Target = target;
                Package = package;
            }
        }
    }
}

using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Server.DoAfter;
using Content.Shared.Stacks;
using System.Reflection.PortableExecutable;

namespace Content.Server.VendingMachines.Restock
{
    public sealed class VendingMachineRestockSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<RestockCompleteEvent>(OnRestockComplete);
            SubscribeLocalEvent<RestockCancelledEvent>(OnRestockCancelled);
        }

        private void OnAfterInteract(EntityUid uid, VendingMachineRestockComponent component, AfterInteractEvent args)
        {
            if (args.Target is null || !args.CanReach)
                return;

            TryRestock(component, args.Target.Value, args.User);
        }

        private bool TryRestock(VendingMachineRestockComponent component, EntityUid target, EntityUid user)
        {
            //check if vending machine
            if (!TryComp<VendingMachineComponent>(target, out var machine))
                return false;

            //check if correct vending machine
            if (!machine.PackPrototypeId.Equals(component.PackPrototypeId))
            {
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-component-invalid"), target, Filter.Entities(user));
                return false;
            }

            var doargs = new DoAfterEventArgs(user, component.RestockDelay, default, target)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 0.1f,
                BroadcastFinishedEvent = new RestockCompleteEvent(component, machine, user),
                BroadcastCancelledEvent = new RestockCancelledEvent(component)
            };

            _doAfterSystem.DoAfter(doargs);

            return true;
        }

        private bool DoAfterRestock(VendingMachineRestockComponent component, VendingMachineComponent machine, EntityUid user)
        {
            if (!_prototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
                return false;

            foreach (var (id, entry) in machine.Inventory)
            {
                if(packPrototype.StartingInventory.ContainsKey(id)) //skip contraband
                    machine.Inventory[id].Amount = packPrototype.StartingInventory[id];
            }

            _audioSystem.Play(component.SoundRestock, Filter.Pvs(machine.Owner), machine.Owner, AudioParams.Default.WithVolume(-2f));
            _popupSystem.PopupEntity(Loc.GetString("vending-machine-restock-component-restocked"), machine.Owner, Filter.Entities(user));

            EntityManager.DeleteEntity(component.Owner);

            return true;
        }

        private void OnRestockCancelled(RestockCancelledEvent ev)
        {
            ev.Component.CancelToken = null;
        }

        private void OnRestockComplete(RestockCompleteEvent ev)
        {
            ev.Component.CancelToken = null;
            DoAfterRestock(ev.Component, ev.Machine, ev.User);
        }

        private sealed class RestockCompleteEvent : EntityEventArgs
        {
            public VendingMachineRestockComponent Component;
            public VendingMachineComponent Machine;
            public EntityUid User;

            public RestockCompleteEvent(VendingMachineRestockComponent component, VendingMachineComponent machine, EntityUid userUid)
            {
                Component = component;
                Machine = machine;
                User = userUid;
            }
        }

        private sealed class RestockCancelledEvent : EntityEventArgs
        {
            public VendingMachineRestockComponent Component;

            public RestockCancelledEvent(VendingMachineRestockComponent component)
            {
                Component = component;
            }
        }
    }
}

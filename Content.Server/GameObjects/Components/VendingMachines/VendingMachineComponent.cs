using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.VendingMachines;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Server.GameObjects.Components.VendingMachines
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class VendingMachineComponent : SharedVendingMachineComponent, IActivate, IExamine, IBreakAct, IWires
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649
        private AppearanceComponent _appearance;
        private BoundUserInterface _userInterface;
        private PowerReceiverComponent _powerReceiver;

        private bool _ejecting = false;
        private TimeSpan _animationDuration = TimeSpan.Zero;
        private string _packPrototypeId;
        private string _description;
        private string _spriteName;

        private bool Powered => _powerReceiver.Powered;
        private bool _broken = false;

        public void Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            if (!Powered)
                return;

            var wires = Owner.GetComponent<WiresComponent>();
            if (wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.playerSession);
            } else
            {
                _userInterface.Open(actor.playerSession);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _packPrototypeId, "pack", string.Empty);
        }

        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) { return; }
            var prototypeManger = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManger.TryIndex(_packPrototypeId, out VendingMachineInventoryPrototype packPrototype))
            {
                return;
            }

            Owner.Name = packPrototype.Name;
            _description = packPrototype.Description;
            _animationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
            _spriteName = packPrototype.SpriteName;
            if (!string.IsNullOrEmpty(_spriteName))
            {
                var spriteComponent = Owner.GetComponent<SpriteComponent>();
                const string vendingMachineRSIPath = "Constructible/Power/VendingMachines/{0}.rsi";
                spriteComponent.BaseRSIPath = string.Format(vendingMachineRSIPath, _spriteName);
            }

            var inventory = new List<VendingMachineInventoryEntry>();
            foreach(var (id, amount) in packPrototype.StartingInventory)
            {
                inventory.Add(new VendingMachineInventoryEntry(id, amount));
            }
            Inventory = inventory;
        }

        public override void Initialize()
        {
            base.Initialize();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(VendingMachineUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            _powerReceiver.OnPowerStateChanged += UpdatePower;
            TrySetVisualState(_powerReceiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off);
            InitializeFromPrototype();
        }

        public override void OnRemove()
        {
            _appearance = null;
            _powerReceiver.OnPowerStateChanged -= UpdatePower;
            _powerReceiver = null;
            base.OnRemove();
        }

        private void UpdatePower(object sender, PowerStateEventArgs args)
        {
            var state = args.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off;
            TrySetVisualState(state);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            var message = serverMsg.Message;
            switch (message)
            {
                case VendingMachineEjectMessage msg:
                    TryEject(msg.ID);
                    break;
                case InventorySyncRequestMessage msg:
                    _userInterface.SendMessage(new VendingMachineInventoryMessage(Inventory));
                    break;
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if(_description == null) { return; }
            message.AddText(_description);
        }

        private void TryEject(string id)
        {
            if (_ejecting || _broken)
            {
                return;
            }

            VendingMachineInventoryEntry entry = Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                FlickDenyAnimation();
                return;
            }

            if (entry.Amount <= 0)
            {
                FlickDenyAnimation();
                return;
            }

            _ejecting = true;
            entry.Amount--;
            _userInterface.SendMessage(new VendingMachineInventoryMessage(Inventory));
            TrySetVisualState(VendingMachineVisualState.Eject);

            Timer.Spawn(_animationDuration, () =>
            {
                _ejecting = false;
                TrySetVisualState(VendingMachineVisualState.Normal);
                Owner.EntityManager.SpawnEntity(id, Owner.Transform.GridPosition);
            });
        }

        private void FlickDenyAnimation()
        {
            TrySetVisualState(VendingMachineVisualState.Deny);
            //TODO: This duration should be a distinct value specific to the deny animation
            Timer.Spawn(_animationDuration, () =>
            {
                TrySetVisualState(VendingMachineVisualState.Normal);
            });
        }

        private void TrySetVisualState(VendingMachineVisualState state)
        {
            var finalState = state;
            if (_broken)
            {
                finalState = VendingMachineVisualState.Broken;
            } else if (_ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            } else if (!Powered)
            {
                finalState = VendingMachineVisualState.Off;
            }
            _appearance.SetData(VendingMachineVisuals.VisualState, finalState);
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true;
            TrySetVisualState(VendingMachineVisualState.Broken);
        }

        public enum Wires
        {
            /// <summary>
            /// Shoots a random item when pulsed.
            /// </summary>
            Shoot
        }

        void IWires.RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Shoot);
        }

        void IWires.WiresUpdate(WiresUpdateEventArgs args)
        {
            var identifier = (Wires) args.Identifier;
            if (identifier == Wires.Shoot && args.Action == WiresAction.Pulse)
            {
                EjectRandom();
            }
        }

        /// <summary>
        /// Ejects a random item if present.
        /// </summary>
        private void EjectRandom()
        {
            var availableItems = Inventory.Where(x => x.Amount > 0).ToList();
            if (availableItems.Count <= 0)
            {
                return;
            }
            TryEject(_random.Pick(availableItems).ID);
        }
    }

    public class WiresUpdateEventArgs : EventArgs
    {
        public readonly object Identifier;
        public readonly WiresAction Action;

        public WiresUpdateEventArgs(object identifier, WiresAction action)
        {
            Identifier = identifier;
            Action = action;
        }
    }

    public interface IWires
    {
        void RegisterWires(WiresComponent.WiresBuilder builder);
        void WiresUpdate(WiresUpdateEventArgs args);

    }
}


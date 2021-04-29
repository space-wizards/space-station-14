#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.VendingMachines;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Server.GameObjects.Components.VendingMachines
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class VendingMachineComponent : SharedVendingMachineComponent, IActivate, IExamine, IBreakAct, IWires
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private bool _ejecting;
        private TimeSpan _animationDuration = TimeSpan.Zero;
        [DataField("pack")]
        private string _packPrototypeId = string.Empty;
        private string? _description;
        private string _spriteName = "";

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private bool _broken;

        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        private string _soundVend = "/Audio/Machines/machine_vend.ogg";
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        private string _soundDeny = "/Audio/Machines/custom_deny.ogg";

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
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
                UserInterface?.Toggle(actor.playerSession);
            }
        }

        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) { return; }
            if (!_prototypeManager.TryIndex(_packPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
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

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                TrySetVisualState(receiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off);
            }

            InitializeFromPrototype();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    UpdatePower(powerChanged);
                    break;
            }
        }

        private void UpdatePower(PowerChangedMessage args)
        {
            var state = args.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off;
            TrySetVisualState(state);

            // Pause/resume advertising if advertising component exists and not broken
            if (!Owner.TryGetComponent(out AdvertiseComponent? advertiseComponent) || _broken) return;

            if (Powered)
            {
                advertiseComponent.Resume();
            }
            else
            {
                advertiseComponent.Pause();
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            var message = serverMsg.Message;
            switch (message)
            {
                case VendingMachineEjectMessage msg:
                    TryEject(msg.ID, serverMsg.Session.AttachedEntity);
                    break;
                case InventorySyncRequestMessage _:
                    UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
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

            var entry = Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                Owner.PopupMessageEveryone(Loc.GetString("Invalid item"));
                Deny();
                return;
            }

            if (entry.Amount <= 0)
            {
                Owner.PopupMessageEveryone(Loc.GetString("Out of stock"));
                Deny();
                return;
            }

            _ejecting = true;
            entry.Amount--;
            UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
            TrySetVisualState(VendingMachineVisualState.Eject);

            Owner.SpawnTimer(_animationDuration, () =>
            {
                _ejecting = false;
                TrySetVisualState(VendingMachineVisualState.Normal);
                Owner.EntityManager.SpawnEntity(id, Owner.Transform.Coordinates);
            });

            SoundSystem.Play(Filter.Pvs(Owner), _soundVend, Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void TryEject(string id, IEntity? sender)
        {
            if (Owner.TryGetComponent<AccessReader>(out var accessReader))
            {
                if (sender == null || !accessReader.IsAllowed(sender))
                {
                    Owner.PopupMessageEveryone(Loc.GetString("Access denied"));
                    Deny();
                    return;
                }
            }
            TryEject(id);
        }

        private void Deny()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _soundDeny, Owner, AudioParams.Default.WithVolume(-2f));

            // Play the Deny animation
            TrySetVisualState(VendingMachineVisualState.Deny);
            //TODO: This duration should be a distinct value specific to the deny animation
            Owner.SpawnTimer(_animationDuration, () =>
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
            }
            else if (_ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (!Powered)
            {
                finalState = VendingMachineVisualState.Off;
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true;
            TrySetVisualState(VendingMachineVisualState.Broken);

            if (Owner.TryGetComponent(out AdvertiseComponent? advertiseComponent))
            {
                advertiseComponent.Pause();
            }
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


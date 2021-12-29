using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.WireHacking;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Acts;
using Content.Shared.Interaction;
using Content.Shared.Sound;
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
using Robust.Shared.ViewVariables;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Popups;
using static Content.Shared.Wires.SharedWiresComponent;

namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class VendingMachineComponent : SharedVendingMachineComponent, IActivate, IBreakAct, IWires, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private bool _ejecting;
        private TimeSpan _animationDuration = TimeSpan.Zero;
        [DataField("pack")]
        private string _packPrototypeId = string.Empty;
        private string _spriteName = "";

        private bool Powered => !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;
        private bool _broken;

        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        private SoundSpecifier _soundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        private SoundSpecifier _soundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);

        [DataField("insertWhitelist")]
        public EntityWhitelist? Whitelist;
        private Container? _storage;

        public bool Broken => _broken;

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                return;
            }
            if (!Powered)
                return;

            var wires = _entMan.GetComponent<WiresComponent>(Owner);
            if (wires.IsPanelOpen)
            {
                wires.OpenInterface(actor.PlayerSession);
            } else
            {
                UserInterface?.Toggle(actor.PlayerSession);
            }
        }

        private void InitializeFromPrototype()
        {
            if (string.IsNullOrEmpty(_packPrototypeId)) { return; }
            if (!_prototypeManager.TryIndex(_packPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            {
                return;
            }

            _entMan.GetComponent<MetaDataComponent>(Owner).EntityName = packPrototype.Name;
            _animationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);
            _spriteName = packPrototype.SpriteName;
            if (!string.IsNullOrEmpty(_spriteName))
            {
                var spriteComponent = _entMan.GetComponent<SpriteComponent>(Owner);
                const string vendingMachineRSIPath = "Structures/Machines/VendingMachines/{0}.rsi";
                spriteComponent.BaseRSIPath = string.Format(vendingMachineRSIPath, _spriteName);
            }

            var inventory = new List<VendingMachineInventoryEntry>();
            foreach(var (id, amount) in packPrototype.StartingInventory)
            {
                if(!_prototypeManager.TryIndex(id, out EntityPrototype? prototype))
                {
                    continue;
                }
                inventory.Add(new VendingMachineInventoryEntry(id, prototype.Name, null, amount));
            }
            Inventory = inventory;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            if (Whitelist != null) { // If insertables allowed, initiate storage
                _storage = Owner.EnsureContainer<Container>("vendor_entity_container");
            }
            if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver))
            {
                TrySetVisualState(receiver.Powered ? VendingMachineVisualState.Normal : VendingMachineVisualState.Off);
            }

            InitializeFromPrototype();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
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
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            var message = serverMsg.Message;
            switch (message)
            {
                case VendingMachineEjectMessage msg:
                    AuthorizedVend(msg.ID, serverMsg.Session.AttachedEntity);
                    break;
                case InventorySyncRequestMessage _:
                    UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
                    break;
            }
        }

        private void TryEjectVendorItem(VendingMachineInventoryEntry entry)
        {
            _ejecting = true;
            entry.Amount--;
            UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
            TrySetVisualState(VendingMachineVisualState.Eject);

            Owner.SpawnTimer(_animationDuration, () =>
            {
                _ejecting = false;
                TrySetVisualState(VendingMachineVisualState.Normal);
                _entMan.SpawnEntity(entry.ID, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
            });

            SoundSystem.Play(Filter.Pvs(Owner), _soundVend.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void TryEjectStorageItem(VendingMachineInventoryEntry entry)
        {
            if (entry.EntityID == null || _storage == null)
            {
                return;
            }
            var entity = entry.EntityID.Value;
            if (_entMan.EntityExists(entity))
            {
                _ejecting = true;
                Inventory.Remove(entry); // remove entry completely
                UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
                TrySetVisualState(VendingMachineVisualState.Eject);
                Owner.SpawnTimer(_animationDuration, () =>
                {
                    _ejecting = false;
                    TrySetVisualState(VendingMachineVisualState.Normal);
                    _storage.Remove(entity);
                });
                SoundSystem.Play(Filter.Pvs(Owner), _soundVend.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
            }
        }
        private void TryDispense(string id)
        {
            if (_ejecting || _broken)
            {
                return;
            }

            var entry = Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-invalid-item"));
                Deny();
                return;
            }

            if (entry.Amount <= 0)
            {
                Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-out-of-stock"));
                Deny();
                return;
            }
            if (entry.EntityID != null) { // If this item is a stored item, use storage eject
                TryEjectStorageItem(entry);
                return;
            }
            if (entry.ID != null) { // If this item is not a stored entity, eject as a new entity of type
                TryEjectVendorItem(entry);
                return;
            }
            return;
        }

        private bool IsAuthorized(EntityUid? sender)
        {
            if (_entMan.TryGetComponent<AccessReaderComponent?>(Owner, out var accessReader))
            {
                var accessSystem = EntitySystem.Get<AccessReaderSystem>();
                if (sender == null || !accessSystem.IsAllowed(accessReader, sender.Value))
                {
                    Owner.PopupMessageEveryone(Loc.GetString("vending-machine-component-try-eject-access-denied"));
                    Deny();
                    return false;
                }
            }
            return true;
        }

        private void AuthorizedVend(string id, EntityUid? sender)
        {
            if (IsAuthorized(sender))
            {
                TryDispense(id);
            }
            return;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_storage == null || Whitelist == null) {
                return false;
            }
            if (!IsAuthorized(eventArgs.User))
            {
                return false;
            }
            if (_entMan.GetComponent<HandsComponent>(eventArgs.User).GetActiveHand?.Owner is not {Valid: true} itemEntity)
            {
                eventArgs.User.PopupMessage(Loc.GetString("vending-machine-component-interact-using-no-active-hand"));
                return false;
            }
            if (!Whitelist.IsValid(itemEntity))
            {
                return false;
            }
            if (!_entMan.TryGetComponent(itemEntity, typeof(ItemComponent), out var item))
            {
                return false;
            }
            var metaData = _entMan.GetComponent<MetaDataComponent>(item.Owner);
            if (metaData.EntityPrototype == null)
            {
                return false;
            }
            EntityUid ent = item.Owner; //Get the entity of the ItemComponent.
            string id = ent.ToString();
            _storage.Insert(ent);
            VendingMachineInventoryEntry newEntry = new VendingMachineInventoryEntry(ent.ToString(), metaData.EntityName, ent, 1);
            Inventory.Add(newEntry);
            UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
            SoundSystem.Play(Filter.Pvs(Owner), _soundVend.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
            return true;
        }

        private void Deny()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _soundDeny.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));

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

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(VendingMachineVisuals.VisualState, finalState);
            }
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
            TryDispense(_random.Pick(availableItems).ID);
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


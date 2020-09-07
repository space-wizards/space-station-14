using System.Diagnostics;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Serilog;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class BreathMaskComponent : ItemComponent, IConnectableToGasTank
    {
        private InventoryComponent _connectedInventorySystem;
        private GasTankComponent ConnectableGasTank
        {
            get
            {
                if (_connectedInventorySystem == null) return null;
                return _connectedInventorySystem.TryGetSlotItem<GasTankComponent>(EquipmentSlotDefines.Slots.BACKPACK,
                    out var tank) ? tank : null;
            }
        }

        public IEntity EquippedOn { get; private set; }
        public override string Name => "BreathMask";
        public override uint? NetID => ContentNetIDs.BREATH_MASK;
        public GasTankComponent ConnectedGasTank { get; set; }


        public override void Equipped(EquippedEventArgs eventArgs)
        {
            if (eventArgs.Slot != EquipmentSlotDefines.Slots.MASK) return;
            EquippedOn = eventArgs.User;
            _connectedInventorySystem = EquippedOn.GetComponent<InventoryComponent>();
            base.Equipped(eventArgs);
        }

        public override void Unequipped(UnequippedEventArgs eventArgs)
        {
            Disconnect();
        }

        public bool IsConnectable()
        {
            return EquippedOn != null && ConnectableGasTank != null;
        }

        public bool IsConnected()
        {
            return ConnectedGasTank != null;
        }

        public void Connect()
        {
            if (!IsConnectable()) return;
            ConnectableGasTank!.Connectable = this;
            ConnectedGasTank = ConnectableGasTank;
            EquippedOn.PopupMessage(Loc.GetString("Attached to gas tank!"));
        }

        public void Disconnect()
        {
            if (!IsConnected()) return;
            Debug.Assert(ConnectedGasTank != null, nameof(ConnectedGasTank) + " != null");
            ConnectedGasTank.Connectable = null;
            ConnectedGasTank = null;
            EquippedOn.PopupMessage(Loc.GetString("Detached from gas tank!"));
        }


        [Verb]
        private sealed class ConnectMaskVerb : Verb<BreathMaskComponent>
        {
            protected override void GetData(IEntity user, BreathMaskComponent component, VerbData data)
            {
                data.Text = component.ConnectedGasTank == null ? "Gas Tank: Connect" : "Gas Tank: Disconnect";
                data.Visibility = component.IsConnectable() ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, BreathMaskComponent component)
            {
                if (!component.IsConnectable()) return;
                if (component.IsConnected())
                {
                    component.Disconnect();
                }
                else
                {
                    component.Connect();
                }
            }
        }
    }
}

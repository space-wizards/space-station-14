using Content.Server.UserInterface;
using Content.Server.WireHacking;
using Content.Shared.Sound;
using Content.Shared.SmartFridge;
using Content.Server.SmartFridge.systems;
using Robust.Server.GameObjects;

using Content.Shared.Chemistry.Components;
using static Content.Shared.Wires.SharedWiresComponent;

namespace Content.Server.SmartFridge
{
    [RegisterComponent]
    public class SmartFridgeComponent : SharedSmartFridgeComponent, IWires
    {
        public bool Ejecting;
        public TimeSpan AnimationDuration = TimeSpan.Zero;
        [DataField("pack")]
        public string SpriteName = "";
        public bool Broken;

        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);
        public float NonLimitedEjectForce = 7.5f;
        public float NonLimitedEjectRange = 5f;

        // public Dictionary< SmartFridgeListEntry>
        public Dictionary<string, SmartFridgeInventoryEntry>? Inventory { get; private set; }
        public Dictionary<string, SmartFridgeInventoryEntry>? InventoryID { get; private set; }
        // public Dictionary<(int ID, string Name), string>? Inventory { get; private set; }

            //                 if (Inventory != null)
            // {
            //     foreach (var (slot, name) in Inventory)
            //     {
            //         _strippingMenu.AddButton(slot.Name, name, (ev) =>
            //         {
            //             SendMessage(new StrippingInventoryButtonPressed(slot.ID));
            //         });
            //     }

        public enum Wires
        {
            /// <summary>
            /// Shoots a random item when pulsed.
            /// </summary>
            Limiter
        }
        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Limiter);
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            var identifier = (Wires) args.Identifier;
            // if (identifier == Wires.Limiter && args.Action == WiresAction.Pulse)
            // {
            //     EntitySystem.Get<SmartFridgeSystem>().EjectRandom(this.Owner, true, this);
            // }
        }
    }

    [Serializable]

    public struct SmartFridgeInventoryEntry
    {
       public int ID;
       public String Name;
       public List<Solution> Reagents;
       public int Count;
        public SmartFridgeInventoryEntry(int id, String name, List<Solution> reagents, int count)
        {
            ID = id;
            Name = name;
            Reagents = reagents;
            Count = count;
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


using System;
using System.Collections.Generic;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Dispenser
{

    /// <summary>
    /// Shared class for <c>ReagentDispenserComponent</c>. Provides a way for entities to dispense and remove reagents from other entities with SolutionComponents via a user interface.
    /// <para>This is useful for machines such as the chemical dispensers, booze dispensers, or soda dispensers.</para>
    /// <para>The chemicals which may be dispensed are defined by specifying a reagent pack. See <see cref="ReagentDispenserInventoryPrototype"/> for more information on that.</para>
    /// </summary>
    public class SharedReagentDispenserComponent : Component
    {
        public override string Name => "ReagentDispenser";

        /// <summary>
        /// A list of reagents which this may dispense. Defined in yaml prototype, see <see cref="ReagentDispenserInventoryPrototype"/>.
        /// </summary>
        protected readonly List<ReagentDispenserInventoryEntry> Inventory = new();

        [Serializable, NetSerializable]
        public class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasPower;
            public readonly bool HasBeaker;
            public readonly FixedPoint2 BeakerCurrentVolume;
            public readonly FixedPoint2 BeakerMaxVolume;
            public readonly string ContainerName;
            /// <summary>
            /// A list of the reagents which this dispenser can dispense.
            /// </summary>
            public readonly List<ReagentDispenserInventoryEntry> Inventory;
            /// <summary>
            /// A list of the reagents and their amounts within the beaker/reagent container, if applicable.
            /// </summary>
            public readonly List<Components.Solution.ReagentQuantity>? ContainerReagents;
            public readonly string DispenserName;
            public readonly FixedPoint2 SelectedDispenseAmount;

            public ReagentDispenserBoundUserInterfaceState(bool hasPower, bool hasBeaker, FixedPoint2 beakerCurrentVolume, FixedPoint2 beakerMaxVolume, string containerName,
                List<ReagentDispenserInventoryEntry> inventory, string dispenserName, List<Components.Solution.ReagentQuantity>? containerReagents, FixedPoint2 selectedDispenseAmount)
            {
                HasPower = hasPower;
                HasBeaker = hasBeaker;
                BeakerCurrentVolume = beakerCurrentVolume;
                BeakerMaxVolume = beakerMaxVolume;
                ContainerName = containerName;
                Inventory = inventory;
                DispenserName = dispenserName;
                ContainerReagents = containerReagents;
                SelectedDispenseAmount = selectedDispenseAmount;
            }
        }

        /// <summary>
        /// Message data sent from client to server when a dispenser ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int DispenseIndex; //Index of dispense button / reagent being pressed. Only used when a dispense button is pressed.

            public UiButtonPressedMessage(UiButton button, int dispenseIndex)
            {
                Button = button;
                DispenseIndex = dispenseIndex;
            }
        }

        [Serializable, NetSerializable]
        public enum ReagentDispenserUiKey
        {
            Key
        }

        /// <summary>
        /// Used in <see cref="UiButtonPressedMessage"/> to specify which button was pressed.
        /// </summary>
        public enum UiButton
        {
            Eject,
            Clear,
            SetDispenseAmount1,
            SetDispenseAmount5,
            SetDispenseAmount10,
            SetDispenseAmount15,
            SetDispenseAmount20,
            SetDispenseAmount25,
            SetDispenseAmount30,
            SetDispenseAmount50,
            SetDispenseAmount100,
            /// <summary>
            /// Used when any dispense button is pressed. Such as "Carbon", or "Oxygen" buttons on the chem dispenser.
            /// The index of the reagent attached to that dispense button is sent as <see cref="UiButtonPressedMessage.DispenseIndex"/>.
            /// </summary>
            Dispense
        }

        /// <summary>
        /// Information about a reagent which the dispenser can dispense.
        /// </summary>
        [Serializable, NetSerializable]
        public struct ReagentDispenserInventoryEntry
        {
            public readonly string ID;

            public ReagentDispenserInventoryEntry(string id)
            {
                ID = id;
            }
        }
    }
}

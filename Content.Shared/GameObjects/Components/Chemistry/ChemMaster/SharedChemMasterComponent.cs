#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{

    /// <summary>
    /// Shared class for <c>ChemMasterComponent</c>. Provides a way for entities to split reagents from a beaker and produce pills, patches, and bottles via a user interface.
    /// </summary>
    public class SharedChemMasterComponent : Component
    {
        public override string Name => "ChemMaster";

        [Serializable, NetSerializable]
        public class ChemMasterBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasBeaker;
            public readonly ReagentUnit BeakerCurrentVolume;
            public readonly ReagentUnit BeakerMaxVolume;
            public readonly string ContainerName;

            /// <summary>
            /// A list of the reagents and their amounts within the beaker/reagent container, if applicable.
            /// </summary>
            public readonly List<Solution.ReagentQuantity> ContainerReagents;
            /// <summary>
            /// A list of the reagents and their amounts within the buffer, if applicable.
            /// </summary>
            public readonly List<Solution.ReagentQuantity> BufferReagents;
            public readonly string DispenserName;

            public ChemMasterBoundUserInterfaceState(bool hasBeaker, ReagentUnit beakerCurrentVolume, ReagentUnit beakerMaxVolume, string containerName,
                string dispenserName, List<Solution.ReagentQuantity> containerReagents, List<Solution.ReagentQuantity> bufferReagents)
            {
                HasBeaker = hasBeaker;
                BeakerCurrentVolume = beakerCurrentVolume;
                BeakerMaxVolume = beakerMaxVolume;
                ContainerName = containerName;
                DispenserName = dispenserName;
                ContainerReagents = containerReagents;
                BufferReagents = bufferReagents;
            }
        }

        /// <summary>
        /// Message data sent from client to server when a ChemMaster ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiActionMessage : BoundUserInterfaceMessage
        {
            /*public readonly UiButton Button;
            public readonly int DispenseIndex; //Index of dispense button / reagent being pressed. Only used when a dispense button is pressed.

            public UiButtonPressedMessage(UiButton button, int dispenseIndex)
            {
                Button = button;
                DispenseIndex = dispenseIndex;
            }*/

            public readonly UiAction action;
            public readonly ReagentUnit amount;
            public readonly string id;
            public readonly bool isBuffer;

            public UiActionMessage(UiAction _action, ReagentUnit? _amount, string? _id, bool? _isBuffer)
            {
                action = _action;
                if (action == UiAction.ChemButton)
                {
                    amount = _amount.GetValueOrDefault();
                    if (_id == null)
                    {
                        id = "";
                    }
                    else
                    {
                        id = _id;
                    }

                    isBuffer = _isBuffer.GetValueOrDefault();
                }
            }
        }

        [Serializable, NetSerializable]
        public enum ChemMasterUiKey
        {
            Key
        }

        /// <summary>
        /// Used in <see cref="UiButtonPressedMessage"/> to specify which button was pressed.
        /// </summary>
        public enum UiAction
        {
            Eject,
            Transfer,
            Discard,
            ChemButton,
        }

    }
}

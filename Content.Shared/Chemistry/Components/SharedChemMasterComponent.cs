using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cloning;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{

    /// <summary>
    /// Shared class for <c>ChemMasterComponent</c>. Provides a way for entities to split reagents from a beaker and produce pills and bottles via a user interface.
    /// </summary>
    public class SharedChemMasterComponent : Component
    {
        public override string Name => "ChemMaster";
        public const string SolutionName = "buffer";

        [Serializable, NetSerializable]
        public class ChemMasterBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasPower;
            public readonly bool HasBeaker;
            public readonly FixedPoint2 BeakerCurrentVolume;
            public readonly FixedPoint2 BeakerMaxVolume;
            public readonly string ContainerName;

            /// <summary>
            /// A list of the reagents and their amounts within the beaker/reagent container, if applicable.
            /// </summary>
            public readonly IReadOnlyList<Solution.ReagentQuantity> ContainerReagents;
            /// <summary>
            /// A list of the reagents and their amounts within the buffer, if applicable.
            /// </summary>
            public readonly IReadOnlyList<Solution.ReagentQuantity> BufferReagents;
            public readonly string DispenserName;

            public readonly bool BufferModeTransfer;

            public readonly FixedPoint2 BufferCurrentVolume;

            public ChemMasterBoundUserInterfaceState(bool hasPower, bool hasBeaker, FixedPoint2 beakerCurrentVolume, FixedPoint2 beakerMaxVolume, string containerName,
                string dispenserName, IReadOnlyList<Solution.ReagentQuantity> containerReagents, IReadOnlyList<Solution.ReagentQuantity> bufferReagents, bool bufferModeTransfer, FixedPoint2 bufferCurrentVolume)
            {
                HasPower = hasPower;
                HasBeaker = hasBeaker;
                BeakerCurrentVolume = beakerCurrentVolume;
                BeakerMaxVolume = beakerMaxVolume;
                ContainerName = containerName;
                DispenserName = dispenserName;
                ContainerReagents = containerReagents;
                BufferReagents = bufferReagents;
                BufferModeTransfer = bufferModeTransfer;
                BufferCurrentVolume = bufferCurrentVolume;
            }
        }

        /// <summary>
        /// Message data sent from client to server when a ChemMaster ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction action;
            public readonly FixedPoint2 amount;
            public readonly string id = "";
            public readonly bool isBuffer;
            public readonly int pillAmount;
            public readonly int bottleAmount;

            public UiActionMessage(UiAction _action, FixedPoint2? _amount, string? _id, bool? _isBuffer, int? _pillAmount, int? _bottleAmount)
            {
                action = _action;
                if (action == UiAction.ChemButton)
                {
                    amount = _amount.GetValueOrDefault();
                    if (_id == null)
                    {
                        id = "null";
                    }
                    else
                    {
                        id = _id;
                    }

                    isBuffer = _isBuffer.GetValueOrDefault();
                }
                else
                {
                    pillAmount = _pillAmount.GetValueOrDefault();
                    bottleAmount = _bottleAmount.GetValueOrDefault();
                }
            }
        }

        [Serializable, NetSerializable]
        public enum ChemMasterUiKey
        {
            Key
        }

        /// <summary>
        /// Used in <see cref="AcceptCloningChoiceMessage"/> to specify which button was pressed.
        /// </summary>
        public enum UiAction
        {
            Eject,
            Transfer,
            Discard,
            ChemButton,
            CreatePills,
            CreateBottles
        }

    }
}

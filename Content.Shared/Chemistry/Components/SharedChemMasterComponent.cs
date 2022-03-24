using System;
using System.Collections.Generic;
using Content.Shared.Cloning;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Components
{

    /// <summary>
    /// Shared class for <c>ChemMasterComponent</c>. Provides a way for entities to split reagents from a beaker and produce pills and bottles via a user interface.
    /// </summary>
    [Virtual]
    public class SharedChemMasterComponent : Component
    {
        [DataField("beakerSlot")]
        public ItemSlot BeakerSlot = new();
        public const string SolutionName = "buffer";

        [Serializable, NetSerializable]
        public sealed class ChemMasterBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasPower;
            public readonly bool HasBeaker;
            public readonly FixedPoint2 BeakerCurrentVolume;
            public readonly FixedPoint2 BeakerMaxVolume;
            public readonly string ContainerName;
            public readonly string Label;

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
            public readonly uint SelectedPillType;

            public ChemMasterBoundUserInterfaceState(bool hasPower, bool hasBeaker, FixedPoint2 beakerCurrentVolume, FixedPoint2 beakerMaxVolume, string containerName, string label,
                string dispenserName, IReadOnlyList<Solution.ReagentQuantity> containerReagents, IReadOnlyList<Solution.ReagentQuantity> bufferReagents, bool bufferModeTransfer, FixedPoint2 bufferCurrentVolume, uint selectedPillType)
            {
                HasPower = hasPower;
                HasBeaker = hasBeaker;
                BeakerCurrentVolume = beakerCurrentVolume;
                BeakerMaxVolume = beakerMaxVolume;
                ContainerName = containerName;
                Label = label;
                DispenserName = dispenserName;
                ContainerReagents = containerReagents;
                BufferReagents = bufferReagents;
                BufferModeTransfer = bufferModeTransfer;
                BufferCurrentVolume = bufferCurrentVolume;
                SelectedPillType = selectedPillType;
            }
        }

        /// <summary>
        /// Message data sent from client to server when a ChemMaster ui button is pressed.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class UiActionMessage : BoundUserInterfaceMessage
        {
            public readonly UiAction Action;
            public readonly FixedPoint2 Amount;
            public readonly string Id = "";
            public readonly bool IsBuffer;
            public readonly string Label = "";
            public readonly uint PillType;
            public readonly int PillAmount;
            public readonly int BottleAmount;

            public UiActionMessage(UiAction action, FixedPoint2? amount, string? id, bool? isBuffer, string? label, uint? pillType, int? pillAmount, int? bottleAmount)
            {
                Action = action;
                if (Action == UiAction.ChemButton)
                {
                    Amount = amount.GetValueOrDefault();
                    if (id == null)
                    {
                        Id = "null";
                    }
                    else
                    {
                        Id = id;
                    }

                    IsBuffer = isBuffer.GetValueOrDefault();
                }
                else
                {
                    PillAmount = pillAmount.GetValueOrDefault();
                    PillType = pillType.GetValueOrDefault();
                    BottleAmount = bottleAmount.GetValueOrDefault();

                    if (label == null)
                    {
                        Label = "null";
                    }
                    else
                    {
                        Label = label;
                    }
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
            CreateBottles,
            SetPillType
        }
    }
}

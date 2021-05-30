#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
{
    public class SharedGasCanisterComponent : Component
    {
        public override string Name => "GasCanister";

        /// <summary>
        /// Key representing which <see cref="BoundUserInterface"/> is currently open.
        /// Useful when there are multiple UI for an object. Here it's future-proofing only.
        /// </summary>
        [Serializable, NetSerializable]
        public enum GasCanisterUiKey
        {
            Key,
        }
    }

    #region Enums

    /// <summary>
    /// Enum representing a UI button.
    /// </summary>
    [Serializable, NetSerializable]
    public enum UiButton
    {
        ValveToggle
    }

    /// <summary>
    /// Used in <see cref="GasCanisterVisualizer"/> to determine which visuals to update.
    /// </summary>
    [Serializable, NetSerializable]
    public enum GasCanisterVisuals
    {
        ConnectedState,
        PressureState
    }

    #endregion

    /// <summary>
    /// Represents a <see cref="GasCanisterComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class GasCanisterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Label;
        public readonly float Volume;
        public readonly float ReleasePressure;
        public readonly bool ValveOpened;

        public GasCanisterBoundUserInterfaceState(string newLabel, float volume, float releasePressure, bool valveOpened)
        {
            Label = newLabel;
            Volume = volume;
            ReleasePressure = releasePressure;
            ValveOpened = valveOpened;
        }

        public bool Equals(GasCanisterBoundUserInterfaceState? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Label == other.Label &&
                   Volume.Equals(other.Volume) &&
                   ReleasePressure.Equals(other.ReleasePressure) &&
                   ValveOpened == other.ValveOpened;
        }
    }

    #region NetMessages

    /// <summary>
    /// Message sent from the client to the server when a gas canister button is pressed
    /// </summary>
    [Serializable, NetSerializable]
    public class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;

        public UiButtonPressedMessage(UiButton button)
        {
            Button = button;
        }
    }

    /// <summary>
    /// Message sent when the release pressure is changed client side
    /// </summary>
    [Serializable, NetSerializable]
    public class ReleasePressureButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly float ReleasePressure;

        public ReleasePressureButtonPressedMessage(float val) : base()
        {
            ReleasePressure = val;
        }
    }

    /// <summary>
    /// Message sent when the canister label has been changed
    /// </summary>
    [Serializable, NetSerializable]
    public class CanisterLabelChangedMessage : BoundUserInterfaceMessage
    {
        public readonly string NewLabel;

        public CanisterLabelChangedMessage(string newLabel) : base()
        {
            NewLabel = newLabel;
        }
    }

    #endregion
}

using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Disposal
{
    /// <summary>
    /// Message data sent from client to server when a disposal unit ui button is pressed.
    /// </summary>
    [Serializable, NetSerializable]
    public class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;

        public UiButtonPressedMessage(UiButton button)
        {
            Button = button;
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Eject,
            Engage,
            Power
        }
    }
}

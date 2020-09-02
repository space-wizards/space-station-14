using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedAcceptCloningComponent : Component
    {
        public override string Name => "AcceptCloning";

        [Serializable, NetSerializable]
        public enum AcceptCloningUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Accept
        }

        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }

    }
}

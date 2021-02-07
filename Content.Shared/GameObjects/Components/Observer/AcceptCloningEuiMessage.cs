using System;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer
{
    [Serializable, NetSerializable]
    public enum AcceptCloningUiButton
    {
        Deny,
        Accept,
    }

    [Serializable, NetSerializable]
    public class AcceptCloningChoiceMessage : EuiMessageBase
    {
        public readonly AcceptCloningUiButton Button;

        public AcceptCloningChoiceMessage(AcceptCloningUiButton button)
        {
            Button = button;
        }
    }
}

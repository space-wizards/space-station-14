using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Cloning
{
    [Serializable, NetSerializable]
    public enum AcceptCloningUiButton
    {
        Deny,
        Accept,
    }

    [Serializable, NetSerializable]
    public sealed class AcceptCloningChoiceMessage : EuiMessageBase
    {
        public readonly AcceptCloningUiButton Button;

        public AcceptCloningChoiceMessage(AcceptCloningUiButton button)
        {
            Button = button;
        }
    }
}

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Revolutionary;

[Serializable, NetSerializable]
public enum BecomeRevUiButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class BecomeRevChoiceMessage : EuiMessageBase
{
    public readonly BecomeRevUiButton Button;

    public BecomeRevChoiceMessage(BecomeRevUiButton button)
    {
        Button = button;
    }
}

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.CryoSleep;

[Serializable, NetSerializable]
public enum AcceptCryoUiButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptCryoChoiceMessage : EuiMessageBase
{
    public readonly AcceptCryoUiButton Button;

    public AcceptCryoChoiceMessage(AcceptCryoUiButton button)
    {
        Button = button;
    }
}

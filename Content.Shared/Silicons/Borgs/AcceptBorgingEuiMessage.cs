using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs;

[Serializable, NetSerializable]
public sealed class AcceptBorgingEuiMessage : EuiMessageBase
{
    public readonly bool Accepted;

    public AcceptBorgingEuiMessage(bool accepted)
    {
        Accepted = accepted;
    }
}

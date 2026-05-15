using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost;

[Serializable, NetSerializable]
public sealed class ReturnToBodyMessage : EuiMessageBase
{
    public readonly bool Accepted;

    public ReturnToBodyMessage(bool accepted)
    {
        Accepted = accepted;
    }
}

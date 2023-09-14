using Robust.Shared.Serialization;

namespace Content.Shared.Eui
{
    [Serializable]
    public abstract class EuiMessageBase
    {

    }

    [Serializable, NetSerializable]
    public sealed class CloseEuiMessage : EuiMessageBase
    {
    }
}

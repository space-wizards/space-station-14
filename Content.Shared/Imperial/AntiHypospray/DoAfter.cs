using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AntiHypo
{
    [Serializable, NetSerializable]
    public sealed partial class HypoDoAfterEvent : SimpleDoAfterEvent
    {
    }
}

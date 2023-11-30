using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Systems;

/// <summary>
///     Raised after welding do_after has finished. It doesn't guarantee success,
///     use <see cref="WeldableChangedEvent"/> to get updated status.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class WeldFinishedEvent : SimpleDoAfterEvent
{
}

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools;

/// <summary>
/// is called after a successful attempt at slicing food.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SliceableDoafterEvent : SimpleDoAfterEvent
{
}

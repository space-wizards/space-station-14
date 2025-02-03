using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition;

[Serializable, NetSerializable]
public sealed partial class ButcherDoafterEvent : SimpleDoAfterEvent;

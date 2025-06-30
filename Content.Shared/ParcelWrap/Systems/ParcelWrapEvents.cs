using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ParcelWrap.Systems;

[Serializable, NetSerializable]
public sealed partial class ParcelWrapItemDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class UnwrapWrappedParcelDoAfterEvent : SimpleDoAfterEvent;

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[Serializable, NetSerializable]
public sealed partial class AbsorbDNADoAfterEvent : SimpleDoAfterEvent { }

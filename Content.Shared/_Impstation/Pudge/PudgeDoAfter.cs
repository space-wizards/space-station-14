using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Pudge;

[Serializable, NetSerializable]
public sealed partial class PudgeDismemberDoAfterEvent : SimpleDoAfterEvent { }

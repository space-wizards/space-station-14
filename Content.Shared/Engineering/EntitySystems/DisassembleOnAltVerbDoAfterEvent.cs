using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Engineering.EntitySystems;

[Serializable, NetSerializable]
public sealed partial class DisassembleOnAltVerbDoAfterEvent : SimpleDoAfterEvent { }

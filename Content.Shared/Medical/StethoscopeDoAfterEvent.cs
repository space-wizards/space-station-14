using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed partial class StethoscopeDoAfterEvent : SimpleDoAfterEvent;

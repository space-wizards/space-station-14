using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Forensics.Events;

[Serializable, NetSerializable]
public sealed partial class CleanForensicsDoAfterEvent : SimpleDoAfterEvent;


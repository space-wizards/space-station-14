using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.TapeRecorder.Events;

[Serializable, NetSerializable]
public sealed partial class TapeCassetteRepairDoAfterEvent : SimpleDoAfterEvent
{
}

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Anomaly;

[Serializable, NetSerializable]
public sealed partial class ScannerDoAfterEvent : SimpleDoAfterEvent
{
}

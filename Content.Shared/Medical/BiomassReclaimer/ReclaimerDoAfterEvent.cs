using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.BiomassReclaimer;

[Serializable, NetSerializable]
public sealed partial class ReclaimerDoAfterEvent : SimpleDoAfterEvent;

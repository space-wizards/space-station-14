using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.RussStation.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryStepDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SurgeryCauteryDoAfterEvent : SimpleDoAfterEvent;

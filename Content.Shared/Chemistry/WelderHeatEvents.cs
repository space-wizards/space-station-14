using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

[Serializable, NetSerializable]
public sealed partial class WelderHeatDoAfterEvent : SimpleDoAfterEvent { }

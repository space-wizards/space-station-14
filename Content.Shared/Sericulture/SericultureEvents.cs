using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Sericulture;

[Serializable, NetSerializable]
public sealed partial class SericultureDoAfterEvent : SimpleDoAfterEvent { }

public sealed partial class SericultureActionEvent : InstantActionEvent { }

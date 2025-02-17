using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Revolutionary;

public sealed partial class HeadRevConvertActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class NewRevStageEvent : EntityEventArgs { }

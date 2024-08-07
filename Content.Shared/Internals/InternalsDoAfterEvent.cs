using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Internals;

[Serializable, NetSerializable]
public sealed partial class InternalsDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ToggleInternalsAlertEvent : BaseAlertEvent;

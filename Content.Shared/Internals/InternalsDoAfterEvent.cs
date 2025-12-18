using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Internals;

public enum ToggleMode
{
    Toggle,
    On,
    Off
}

[Serializable, NetSerializable]
public sealed partial class InternalsDoAfterEvent : DoAfterEvent
{
    public ToggleMode ToggleMode = ToggleMode.Toggle;

    public InternalsDoAfterEvent(ToggleMode mode)
    {
        ToggleMode = mode;
    }

    public override DoAfterEvent Clone() => this;
}

public sealed partial class ToggleInternalsAlertEvent : BaseAlertEvent;

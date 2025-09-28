using Content.Shared.Actions;

namespace Content.Shared._Starlight.Actions.Events;

public sealed partial class OpenUplinkImplantEvent : InstantActionEvent
{
    [ViewVariables]
    public EntityUid User { get; set; }
}
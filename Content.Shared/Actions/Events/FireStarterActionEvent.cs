namespace Content.Shared.Actions.Events;

public sealed partial class FireStarterActionEvent : InstantActionEvent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Severity = 0.3f;
}

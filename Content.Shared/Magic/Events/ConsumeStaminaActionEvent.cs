using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

/// <summary>
/// Consumes a fixed amount of stamina from the performer when the InstantAction is executed.
/// </summary>
public sealed partial class ConsumeStaminaActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public float Amount;
}

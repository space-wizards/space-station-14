using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

/// <summary>
/// Adds provided Charge to the held wand
/// </summary>
public sealed partial class ChargeSpellEvent : InstantActionEvent
{
    [DataField(required: true)]
    public int Charge;

    [DataField]
    public string WandTag = "WizardWand";
}

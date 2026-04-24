using Content.Shared.Actions;
using Content.Shared.Whitelist;

namespace Content.Shared.Magic.Events;

/// <summary>
/// Adds provided Charge to the held wand
/// </summary>
public sealed partial class ChargeSpellEvent : InstantActionEvent
{
    /// <summary>
    /// How many charges to refill.
    /// </summary>
    [DataField(required: true)]
    public int Charge;

    /// <summary>
    /// Whitelist for entities that can be recharged.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new();
}

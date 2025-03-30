namespace Content.Server.PhoneBill;

/// <summary>
///     If this entity can recieve phone bills.
/// </summary>
[RegisterComponent]
public sealed partial class PhoneBillableComponent : Component
{
    /// <summary>
    ///     If this entity must be alive (Not crit) to recieve phone bills.
    ///     This only controls if someone will get a phone bill while alive,
    ///     not if they will keep it if they crit/die.
    ///     Basically, if you die after getting a phone bill, you will still
    ///     lose your PDA and ID if you don't got money.
    /// </summary>
    [DataField]
    public bool RequireLiving = true;
}

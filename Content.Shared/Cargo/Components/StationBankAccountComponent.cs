namespace Content.Shared.Cargo.Components;

/// <summary>
/// Added to the abstract representation of a station to track its money.
/// </summary>
[RegisterComponent, Friend(typeof(SharedCargoSystem))]
public sealed class StationBankAccountComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("balance")]
    public int Balance;
}

using Content.Shared.Cargo;
using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Makes an entity a client of the station's bank account.
/// When its balance changes it will have <see cref="BankBalanceUpdatedEvent"/> raised on it.
/// Other systems can then use this for logic or to update ui states.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCargoSystem))]
[AutoGenerateComponentState]
public sealed partial class BankClientComponent : Component
{
    /// <summary>
    /// The balance updated for the last station this entity was a part of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Balance;
}

/// <summary>
/// Raised on an entity with <see cref="BankClientComponent"/> when the bank's balance is updated.
/// </summary>
[ByRefEvent]
public record struct BankBalanceUpdatedEvent(EntityUid Station, int Balance);

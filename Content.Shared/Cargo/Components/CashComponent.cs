using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Can be inserted into a <see cref="CargoOrderConsoleComponent"/> to increase the station's bank account.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CashComponent : Component
{

}

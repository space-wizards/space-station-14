using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Target for approved orders to spawn at.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TradeStationComponent : Component
{
    /// <summary>
    ///     Is the Trade Station currently being hijacked? Modified by CargoSystem.Hack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Hacked = false;
}

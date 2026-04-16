using Content.Shared.HijackBeacon;
using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Target for approved orders to spawn at.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TradeStationComponent : Component
{
    /// <summary>
    ///     The Trade Station's current hijack state. Modified by HijackBeaconSystem.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Hacked = false;
}

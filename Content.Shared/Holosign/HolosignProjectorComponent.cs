using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Holosign;

/// <summary>
/// Added to an item and allows it to spawn a specified prototype at the location you click on, using charge from a power cell.
/// Used for holosigns, holofans and holobarriers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolosignProjectorComponent : Component
{
    /// <summary>
    /// The prototype to spawn on use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId SignProto = "HolosignWetFloor";

    /// <summary>
    /// How much charge a single use expends, in watts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ChargeUse = 50f;

    /// <summary>
    /// Whether or not to use predictive spawning.
    /// At the moment this does not support entities with animated sprites, so set this to false in that case.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PredictedSpawn;
}

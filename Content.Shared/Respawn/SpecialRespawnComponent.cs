using Robust.Shared.GameStates;

namespace Content.Shared.Respawn;

/// <summary>
/// This is to be used where you need some item respawned on station if it was deleted somehow in round
/// Items like the nuke disk.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class SpecialRespawnComponent: Component
{
    [ViewVariables]
    [DataField("station")]
    public EntityUid Station = EntityUid.Invalid;

    [ViewVariables]
    [DataField("stationMap")]
    public (EntityUid?, EntityUid?) StationMap;

    /// <summary>
    /// Checks if the entityentity should respawn on the station grid
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("respawn")]
    public bool Respawn = true;

    /// <summary>
    /// The prototypeID of the entity to be respawned
    /// </summary>
    [ViewVariables]
    [DataField("prototypeID")]
    public string Prototype = "";
}

[ByRefEvent]
public struct SpecialRespawnSetupEvent
{

}

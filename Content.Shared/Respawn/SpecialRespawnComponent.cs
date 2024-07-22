using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Respawn;

/// <summary>
/// This is to be used where you need some item respawned on station if it was deleted somehow in round
/// Items like the nuke disk.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecialRespawnComponent: Component
{
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
    [DataField(required: true)]
    public EntProtoId Prototype = "";
}

public sealed class SpecialRespawnSetupEvent : EntityEventArgs
{
    public EntityUid Entity;

    public SpecialRespawnSetupEvent(EntityUid entity)
    {
        Entity = entity;
    }
}

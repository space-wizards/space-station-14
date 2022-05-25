namespace Content.Server.Station.Components;

[RegisterComponent]
public sealed class StationRespawnComponent : Component
{
    /// <summary>
    /// The station this object is attached to, if any.
    /// </summary>
    [DataField("attachedStation")]
    public EntityUid? AttachedStation = null;

    /// <summary>
    /// Whether or not to alert staff that the object got deleted.
    /// </summary>
    [DataField("alertStaff")]
    public bool AlertStaff = false;

    /// <summary>
    /// Maximum distance from the station this object is allowed to have.
    /// Setting this to null removes the distance cap.
    /// </summary>
    [DataField("maxStationDistance")]
    public float? MaximumStationDistance = 500.0f;

    /// <summary>
    /// The message to display when this object gets respawned, if possible.
    /// </summary>
    [DataField("respawnPopupMessage")]
    public string? RespawnPopupMessage = "station-respawn-default-message";
}

using Robust.Shared.Utility;

namespace Content.Server.Starlight.AlertArmory;

[RegisterComponent]
public sealed partial class AlertArmoryStationComponent : Component
{
    /// <summary>
    /// a dictionary of alert level -> shuttle grid to load
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, AlertArmoryDefinition> Shuttles = [];

    /// <summary>
    /// a dictionary of alert level -> preloaded grid
    /// </summary>
    [ViewVariables]
    public Dictionary<string, EntityUid> Grids = [];
}

[DataDefinition]
public sealed partial class AlertArmoryDefinition
{
    ///<summary>
    /// Path to shuttle grid which will be used for spawn.
    ///</summary>
    [DataField(required: true)]
    public ResPath Shuttle;

    ///<summary>
    /// Announcement message which will be sended when armory fly to station.
    /// </summary>
    [DataField]
    public string? Announcement;

    ///<summary>
    /// Color of announcement message which will be sended when armory fly to station.
    ///</summary>
    [DataField]
    public Color? AnnouncementColor;
}
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
    [DataField(required: true)]
    public ResPath Shuttle;

    [DataField]
    public string? Announcement;

    [DataField]
    public Color? AnnouncementColor;
}
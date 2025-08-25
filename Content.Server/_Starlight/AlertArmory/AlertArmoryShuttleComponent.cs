using Content.Server.Acz;
using Robust.Shared.Map;

namespace Content.Server.Starlight.AlertArmory;

/// <summary>
/// this comp is entirely added by the AlertArmory system and shouldn't be attached.
/// </summary>
[RegisterComponent]
public sealed partial class AlertArmoryShuttleComponent : Component
{
    [DataField]
    public EntityUid Station;

    [DataField]
    public ProtoId<TagPrototype> DockTag = "DockGamma";

    [ViewVariables]
    public string? Announcement = null;

    [ViewVariables]
    public Color? AnnouncementColor = null;

    #region Return to "Armory Space"
    [ViewVariables]
    public EntityCoordinates CoordsCache;

    [ViewVariables]
    public EntityUid ArmorySpaceUid;
    #endregion
}
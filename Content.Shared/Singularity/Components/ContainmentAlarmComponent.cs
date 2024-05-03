using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainmentAlarmComponent : Component
{
    /// <summary>
    /// The radio channel that the normal field announcements are broadcast to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Engineering";

    /// <summary>
    /// The radio channel that the emergency field announcements are broadcast to, aka when it's about to loose.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> EmergencyAnnouncementChannel = "Common";

    /// <summary>
    /// The last power level tracked by this alert system. Alerts happen when it starts losing power and every minute or so.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int LastAlertPowerLevel = -1;

    /// <summary>
    /// The interval of power between alerts
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PowerIntervalBetweenAlerts = 6;

    /// <summary>
    /// The threshold where the channel changes from engineering to common
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int EmergencyThreshold = 12;

    /// <summary>
    /// Used to display power left
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PowerCap = 25;
}

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
    [DataField]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Engineering";

    /// <summary>
    /// The last power level tracked by this alert system. Alerts happen when it starts losing power and every minute or so.
    /// </summary>
    [DataField]
    public int LastAlertPowerLevel = 0;

    /// <summary>
    /// The interval of power between alerts
    /// </summary>
    [DataField]
    public int PowerIntervalBetweenAlerts = 12;

    /// <summary>
    /// Used to display power left
    /// </summary>
    [DataField]
    public int PowerCap = 25;
}

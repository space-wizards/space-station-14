using Content.Server.StationEvents.Events;
using Content.Shared.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Added to gamerule entity prototypes to make them change the station's alert level when starting the gamerule.
/// This will only apply if the station is currently on its default alert level.
/// </summary>
[RegisterComponent, Access(typeof(AlertLevelInterceptionRule))]
public sealed partial class AlertLevelInterceptionRuleComponent : Component
{
    /// <summary>
    /// Alert level to set the station to when the event starts.
    /// </summary>
    [DataField]
    public ProtoId<AlertLevelPrototype> AlertLevel = "Blue";
}

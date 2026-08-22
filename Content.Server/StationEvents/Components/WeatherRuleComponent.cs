using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(WeatherRule))]
public sealed partial class WeatherRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Weather;

    [DataField]
    public TimeSpan MinDuration = TimeSpan.FromMinutes(1f);

    [DataField]
    public TimeSpan MaxDuration = TimeSpan.FromMinutes(5f);
}

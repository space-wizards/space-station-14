using Content.Shared.Weather;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Changes the current weather when triggered.
/// If TargetUser is true then it will change the weather at the user's map instead of the entitys map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeatherOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Weather type. Null to clear the weather.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<WeatherPrototype>? Weather;

    /// <summary>
    /// How long the weather should last. Null for forever.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}

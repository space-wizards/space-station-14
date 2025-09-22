using Content.Shared.Weather;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Changes the current weather when triggered
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class WeatherOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Weather type
    /// </summary>
    [DataField]
    public ProtoId<WeatherPrototype>? Weather;

}

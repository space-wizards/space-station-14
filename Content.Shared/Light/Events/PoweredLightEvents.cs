namespace Content.Shared.Light.Events;

/// <summary>
/// Raised on a powered light entity whenever it is updated in LightBulbState.Normal mode.
/// </summary>
/// <param name="LightState">If set, this will override the normal checks and set the light on/off for true/false.</param>
[ByRefEvent]
public record struct OverridePoweredLightStatus(bool? LightState);

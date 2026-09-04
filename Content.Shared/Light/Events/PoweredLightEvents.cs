namespace Content.Shared.Light.Events;

/// <summary>
/// Raised on a powered light entity whenever it is updated in LightBulbState.Normal mode.
/// </summary>
/// <param name="LightState">If set, this will override the normal checks and set the light on/off for true/false.</param>
[ByRefEvent]
public record struct OverridePoweredLightStatus(bool? LightState);

/// <summary>
/// Raised on a light when its value is updated.
/// </summary>
/// <remarks>
/// Note that this does not guarantee that the value is *changed*, only that the function to set it was called.
/// </remarks>
/// <param name="Value">The value the light was set to.</param>
[ByRefEvent]
public record struct PoweredLightValueUpdated(bool Value);

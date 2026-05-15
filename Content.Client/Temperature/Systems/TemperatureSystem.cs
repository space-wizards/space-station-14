using Content.Shared.Temperature.Systems;

namespace Content.Client.Temperature.Systems;

/// <summary>
/// This exists so <see cref="SharedTemperatureSystem"/> runs on client/>
/// </summary>
public sealed class TemperatureSystem : SharedTemperatureSystem;

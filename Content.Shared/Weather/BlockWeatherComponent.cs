using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// This entity will block the weather if it's anchored to the floor.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockWeatherComponent : Component
{

}

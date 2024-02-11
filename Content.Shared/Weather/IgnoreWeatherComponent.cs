using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

/// <summary>
/// This entity will be ignored for considering weather on a tile
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreWeatherComponent : Component
{

}

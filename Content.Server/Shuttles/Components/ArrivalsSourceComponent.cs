using Content.Server.Shuttles.Systems;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a designated arrivals station for players to spawn at, if enabled.
/// </summary>
[RegisterComponent, Access(typeof(ArrivalsSystem))]
public sealed partial class ArrivalsSourceComponent : Component
{

}

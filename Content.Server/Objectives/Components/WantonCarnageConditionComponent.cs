using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to leave station of escape shuttle with only antags on board or handcuffed humanoids
/// </summary>
[RegisterComponent, Access(typeof(WantonCarnageConditionSystem))]
public sealed partial class WantonCarnageConditionComponent : Component
{
}

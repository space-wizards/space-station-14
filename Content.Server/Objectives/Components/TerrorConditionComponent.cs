using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and has called in a threat.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem))]
public sealed partial class TerrorConditionComponent : Component
{
}

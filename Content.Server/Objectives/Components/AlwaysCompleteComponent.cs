using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is on the emergency shuttle's grid when docking to CentCom.
/// </summary>
[RegisterComponent, Access(typeof(AlwaysCompleteSystem))]
public sealed partial class AlwaysCompleteComponent : Component
{
}

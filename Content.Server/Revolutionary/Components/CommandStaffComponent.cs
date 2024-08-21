using Content.Server.GameTicking.Rules;

namespace Content.Server.Revolutionary.Components;

/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))] //TODO:ERRANT should this be on the command job mindrole?
public sealed partial class CommandStaffComponent : Component
{

}

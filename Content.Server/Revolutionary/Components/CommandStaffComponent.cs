using Content.Server.GameTicking.Rules;

namespace Content.Server.Revolutionary.Components;

/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class CommandStaffComponent : Component
{

}

//TODO this should probably be on a mind role, not the mob

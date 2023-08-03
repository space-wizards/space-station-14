using Content.Server.Flash;
using Content.Server.GameTicking.Rules;
using Robust.Shared.GameStates;

namespace Content.Server.Revolutionary.Components;
/// <summary>
/// Given to heads that decide to run away from station during Revs.
/// </summary>

[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed class ExiledComponent : Component
{
}

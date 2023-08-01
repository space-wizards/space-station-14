using Content.Server.Flash;
using Content.Server.GameTicking.Rules;
using Robust.Shared.GameStates;

namespace Content.Server.Revolutionary;
/// <summary>
/// Given to heads at round start for Revs. Used for tracking heads died or not.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed class HeadComponent : Component
{
}

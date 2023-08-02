using Content.Server.Flash;
using Content.Server.GameTicking.Rules;
using Robust.Shared.GameStates;

namespace Content.Server.Revolutionary.Components;
/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// (Not sure if it matters but this crashes the game if this is networked and I honest to God could not tell you why.)
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed class CommandComponent : Component
{
}

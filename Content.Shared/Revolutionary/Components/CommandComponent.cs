using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;

namespace Content.Server.Revolutionary.Components;
/// <summary>
/// Given to heads at round start for Revs. Used for tracking if heads died or not.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed class CommandComponent : Component
{
}

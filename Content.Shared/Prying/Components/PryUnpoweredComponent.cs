using Robust.Shared.GameStates;

namespace Content.Shared.Prying.Components;

///<summary>
/// Applied to entities that can be pried open without tools while unpowered
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PryUnpoweredComponent : Component
{
}

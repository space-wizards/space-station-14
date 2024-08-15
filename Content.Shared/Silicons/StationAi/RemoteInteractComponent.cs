using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates that InRangeUnobstructed checks should be bypassed for this entity, effectively giving it infinite interaction range.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoteInteractComponent : Component
{

}

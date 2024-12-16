using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is currently being conveyed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConveyedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Colliding = new();
}

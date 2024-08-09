using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Represents a boundary that can bump someone back when touched.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BoundaryComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Offset = 2f;
}

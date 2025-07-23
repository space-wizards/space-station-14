using Robust.Shared.GameStates;

namespace Content.Shared.Physics;

/// <summary>
/// Prevents collision on specific layers while still allowing for start and end collide events to be raised.
/// Effectively makes the marked layers of a specific entity "Soft."
/// Does nothing if the fixture is already soft.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoftFixtureMaskComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Mask;
}

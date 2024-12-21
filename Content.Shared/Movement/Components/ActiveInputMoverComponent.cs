using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Indicates entity is able to tick <see cref="InputMoverComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveInputMoverComponent : Component
{

}

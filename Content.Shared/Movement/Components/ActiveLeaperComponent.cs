using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Marker component given to the users of the <see cref="JumpAbilityComponent"/> if they are meant to collide with each other.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveLeaperComponent : Component;

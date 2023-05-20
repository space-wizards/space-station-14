using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent, NetworkedComponent]
public sealed class GrapplingGunComponent : Component
{

}

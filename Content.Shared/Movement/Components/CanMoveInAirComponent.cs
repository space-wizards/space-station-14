using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Movement.Components;

/// <summary>
///     On mobs that are allowed to move while their body status is <see cref="BodyStatus.InAir"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanMoveInAirComponent : Component
{
}

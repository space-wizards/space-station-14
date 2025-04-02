using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Makes entities with <c>flammable</c>component automatically enable/disable <see cref="CollisionWakeComponent"/>,
/// so they can be extinguished with fire extinguishers.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class FlammableSetCollisionWakeComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared.Explosion.Components.OnTrigger;

/// <summary>
/// Splits into more entities based on the entity's <see cref="ClusterGrenadeComponent"/> if Triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClusterOnTriggerComponent : Component
{
}

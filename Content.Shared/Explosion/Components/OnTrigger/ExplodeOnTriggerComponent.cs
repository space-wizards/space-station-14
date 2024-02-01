using Robust.Shared.GameStates;

namespace Content.Shared.Explosion.Components.OnTrigger;

/// <summary>
/// Explode using the entity's <see cref="ExplosiveComponent"/> if Triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExplodeOnTriggerComponent : Component
{
}

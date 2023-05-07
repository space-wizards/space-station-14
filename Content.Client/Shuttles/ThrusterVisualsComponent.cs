namespace Content.Client.Shuttles;

/// <summary>
/// A component that emits a visible exhaust plume if the entity is an active thruster.
/// Managed by <see cref="ThrusterVisualizerSystem"/>
/// </summary>
[RegisterComponent, Access(typeof(ThrusterVisualizerSystem))]
public sealed class ThrusterVisualsComponent : Component
{
}

using Robust.Shared.GameStates;

namespace Content.Shared.Buckle.Components;

/// <summary>
/// Marker component for entities that are currently applying ignition to buckled entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnBuckleBurningComponent : Component;

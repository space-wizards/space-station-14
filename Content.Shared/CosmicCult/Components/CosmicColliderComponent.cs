using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Marker component for entities that cult-related entities can walk through but are solid to others.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicColliderComponent : Component;

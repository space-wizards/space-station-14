using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Marker component added to entities to prevent them from damaging astral-branded cosmic cultists.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicFriendlyFireBlockerComponent : Component;

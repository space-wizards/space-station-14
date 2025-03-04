using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Marker component for handling The Monument's collision.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MonumentCollisionComponent : Component
{
    /// <summary>
    /// A bool that determines whether The Monument is tangible to non-cultists.
    /// </summary>
    [DataField, AutoNetworkedField] public bool HasCollision = false;
}

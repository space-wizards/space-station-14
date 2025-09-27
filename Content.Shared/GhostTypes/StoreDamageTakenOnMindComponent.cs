using Robust.Shared.GameStates;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Stores the damage an entity took before that body is destroyed in their mind component
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoreDamageTakenOnMindComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Stores the damage an entity took before their body is destroyed inside it's mind LastBodyDamageComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StoreDamageTakenOnMindComponent : Component;

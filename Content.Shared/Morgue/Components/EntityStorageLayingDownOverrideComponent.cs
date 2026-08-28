using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// Makes an entity storage only accept entities that are laying down.
/// This is true for mobs that are crit, dead or crawling.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityStorageLayingDownOverrideComponent : Component;

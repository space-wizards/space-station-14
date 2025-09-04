using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
///     Cleanup component that deletes the entity if it has a cross-grid collision.
///     Useful for small, unimportant items like bullets to avoid generating many contacts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpaceGarbageComponent : Component;

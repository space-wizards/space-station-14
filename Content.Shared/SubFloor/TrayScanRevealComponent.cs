using Robust.Shared.GameStates;

namespace Content.Shared.SubFloor;

/// <summary>
/// For tile-occluding entities, like catwalk, carpets and walls, to reveal subfloor entities
/// when on the same tile and when using a t-ray scanner.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TrayScanRevealComponent : Component;

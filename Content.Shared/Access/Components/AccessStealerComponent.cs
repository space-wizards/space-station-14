using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

/// <summary>
/// Allows an ID card to copy accesses from other IDs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AccessStealerComponent : Component;

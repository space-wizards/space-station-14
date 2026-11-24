using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marker component for entities that have been put inside a fleshtomb before.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecentlyFleshtombedComponent : Component;

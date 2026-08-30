using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marker component for entities that were devoured recently and cannot be devoured again until revived.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecentlyDevouredComponent : Component;

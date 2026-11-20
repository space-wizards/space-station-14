using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// Marker components for child organs that are considered "internal" to their parent. e.g. kidneys are internal to a torso, but an arm isn't.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InternalChildOrganComponent : Component;

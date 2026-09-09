using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marker component for changeling identities that can't be dropped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingUnremovableIdentityComponent : Component;

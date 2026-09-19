using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marker component for changeling identities that don't count for the identity cap.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingUncountedIdentityComponent : Component;

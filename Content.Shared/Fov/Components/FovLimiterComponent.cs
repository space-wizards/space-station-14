using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Fov.Components;

/// <summary>
/// Enables a client-side rendering mask that limits perceived FOV to a cone.
/// Purely visual unless paired with gameplay checks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FovLimiterComponent : Component
{
    /// <summary>
    /// Whether the cone mask is enabled for this entity/local player.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Total cone angle in degrees (e.g., 120).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FovLimit = 180f;

    /// <summary>
    /// If true, apply cone overlay globally for the local client, regardless of which entity owns this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyToAllPlayers = false;
}

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.RepulseAttract;

/// <summary>
///     Used to repulse or attract entities away from the entity this is on
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(RepulseAttractSystem))]
public sealed partial class RepulseAttractComponent : Component
{
    /// <summary>
    ///     Attracts if true, Repulse if false.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Attract;

    /// <summary>
    ///     How fast should the Repulsion/Attraction be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 5.0f;

    /// <summary>
    ///     How close do the entities need to be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 5.0f;

    /// <summary>
    ///     What kind of entities should this effect apply to?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}

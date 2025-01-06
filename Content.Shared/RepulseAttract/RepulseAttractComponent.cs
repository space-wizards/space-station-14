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
    ///     Optional user, used if the <see cref="RepulseAttractComponent"/> is on an Item
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    ///     Attracts if true, Repulse if false.
    /// </summary>
    [DataField]
    public bool Attract;

    /// <summary>
    ///     How strong should the Repulse/Attract be?
    /// </summary>
    [DataField]
    public float Strength = 1.0F;

    /// <summary>
    ///     How close do the entities need to be?
    /// </summary>
    [DataField]
    public float Range = 1.0F;

    // TODO: Add stun check
    // TODO: Add enums for behavior?
    //    On Activate
    //    On Throw
    //    On Hit

    /// <summary>
    ///     What kind of entities should this effect only?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     What kind of entities should be excluded from the effect?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     How long until this can be used again?
    /// </summary>
    [DataField]
    public TimeSpan? UseDelay;

    /// <summary>
    ///     When is the next time this can be used? Determined by <see cref="UseDelay"/>
    /// </summary>
    [DataField]
    public TimeSpan? NextUse;
}

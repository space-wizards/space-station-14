using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Prevents an entity from emitting sounds.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SilencedComponent : Component
{
    /// <summary>
    /// Can this entity make sounds indirectly by interacting with sound emitting objects?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowEmitterUse;

    /// <summary>
    /// Can this entity make footstep sounds?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowFootsteps = true;

    /// <summary>
    /// Can this entity make sounds while eating?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowEatingSounds;

    /// <summary>
    /// Can this entity make sounds while drinking?
    /// </summary>

    [DataField, AutoNetworkedField]
    public bool AllowDrinkingSounds;

    /// <summary>
    /// Used to track if the entity is allowed to make footstep sounds normally.
    /// </summary>
    [AutoNetworkedField]
    public bool HadFootsteps;
}

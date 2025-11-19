using Content.Shared.Power;
using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GravityGeneratorComponent : Component
{
    [DataField] public float LightRadiusMin { get; set; }
    [DataField] public float LightRadiusMax { get; set; }

    /// <summary>
    /// A map of the sprites used by the gravity generator given its status.
    /// </summary>
    [DataField, Access(typeof(SharedGravitySystem))]
    public Dictionary<PowerChargeStatus, string> SpriteMap = [];

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is starting up.
    /// </summary>
    [DataField]
    public string CoreStartupState = "startup";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is idle.
    /// </summary>
    [DataField]
    public string CoreIdleState = "idle";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is activating.
    /// </summary>
    [DataField]
    public string CoreActivatingState = "activating";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is active.
    /// </summary>
    [DataField]
    public string CoreActivatedState = "activated";

    /// <summary>
    /// Is the gravity generator currently "producing" gravity?
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedGravityGeneratorSystem))]
    public bool GravityActive = false;
}

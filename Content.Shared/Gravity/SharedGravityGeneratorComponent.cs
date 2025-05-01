using Content.Shared.Power;
using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

[NetworkedComponent()]
[Virtual]
public partial class SharedGravityGeneratorComponent : Component
{
    /// <summary>
    /// A map of the sprites used by the gravity generator given its status.
    /// </summary>
    [DataField("spriteMap")]
    [Access(typeof(SharedGravitySystem))]
    public Dictionary<PowerChargeStatus, string> SpriteMap = new();

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is starting up.
    /// </summary>
    [DataField("coreStartupState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string CoreStartupState = "startup";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is idle.
    /// </summary>
    [DataField("coreIdleState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string CoreIdleState = "idle";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is activating.
    /// </summary>
    [DataField("coreActivatingState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string CoreActivatingState = "activating";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is active.
    /// </summary>
    [DataField("coreActivatedState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string CoreActivatedState = "activated";
}

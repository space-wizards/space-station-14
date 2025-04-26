using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class KnockedDownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public bool AutoStand = true;

    [DataField("helpInterval"), AutoNetworkedField]
    public float HelpInterval = 1f;

    [DataField("helpAttemptSound")]
    public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [ViewVariables, AutoNetworkedField]
    public float HelpTimer = 0f;

    /// <summary>
    /// Friction modifier for knocked down players.
    /// Makes them accelerate and deccelerate slower.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 0.4f; // Should add a friction modifier to slipping to compensate for this

    /// <summary>
    /// Modifier to the maximum movement speed of a knocked down mover.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.3f;
}

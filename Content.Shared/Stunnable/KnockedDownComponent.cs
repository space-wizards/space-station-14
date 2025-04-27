using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class KnockedDownComponent : Component
{
    /// <summary>
    /// Game time that we can stand up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Should we try to stand up?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoStand = true;

    /// <summary>
    /// The Standing Up DoAfter.
    /// </summary>
    [DataField]
    public DoAfterId? DoAfter;

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

    /// <summary>
    /// How long does it take us to get up?
    /// </summary>
    public TimeSpan GetUpDoAfter = TimeSpan.FromSeconds(1);

    // TODO: This isn't my code reuse if able, prune if necessary
    [DataField("helpAttemptSound")]
    public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField("helpInterval"), AutoNetworkedField]
    public float HelpInterval = 1f;

    [ViewVariables, AutoNetworkedField]
    public float HelpTimer = 0f;
}

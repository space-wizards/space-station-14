using Content.Shared.Turnstile.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Turnstile.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TurnstileComponent : Component
{
    /// <summary>
    /// The current state of the turnstile -- whether it is idle or rotating.
    /// </summary>
    /// <remarks>
    /// This should never be set directly, use <see cref="SharedTurnstileSystem.SetState(EntityUid, TurnstileState, TurnstileComponent?)"/> instead.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    [Access(typeof(SharedTurnstileSystem))]
    public TurnstileState State = TurnstileState.Idle;

    /// <summary>
    /// The current entity being admitted by the turnstile.
    /// </summary>
    /// <remarks>
    /// This should never be set directly.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    [Access(typeof(SharedTurnstileSystem))]
    public EntityUid CurrentlyAdmittingEntity;


    #region Timing
    /// <summary>
    /// Time between each turn of the turnstile. Allows controlling the "flow rate" of mobs through.
    /// </summary>
    [DataField]
    public TimeSpan TurnstileTurnTime = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    ///     When the turnstile is rotating, this is the time when the state will next update.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? NextStateChange;

    #endregion

    #region Sounds
    /// <summary>
    /// Sound to play when the turnstile admits a mob through.
    /// </summary>
    [DataField("turnSound")]
    public SoundSpecifier? TurnSound;

    /// <summary>
    /// Sound to play when the turnstile is bumped from the wrong side
    /// </summary>
    [DataField("bumpSound")]
    public SoundSpecifier? BumpSound;

    #endregion

    #region Graphics
    /// <summary>
    /// The key used when playing turnstile rotation animations.
    /// </summary>
    public const string AnimationKey = "turnstile_animation";

    /// <summary>
    /// The sprite state used for the turnstile at rest.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string IdleSpriteState = "idle";

    /// <summary>
    /// The sprite state used for the turnstile performing one rotation.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string RotationSpriteState = "rotation";

    /// <summary>
    /// The animation used when the turnstile turns
    /// </summary>
    public object RotatingAnimation = default!;

    #endregion

}

[Serializable, NetSerializable]
public enum TurnstileState : byte
{
    Idle,
    Rotating
}

[Serializable, NetSerializable]
public enum TurnstileVisuals : byte
{
    State,
    BaseRSI
}

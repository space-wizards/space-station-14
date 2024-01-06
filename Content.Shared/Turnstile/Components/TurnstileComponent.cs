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
    [DataField]
    public SoundSpecifier? TurnSound;

    /// <summary>
    /// Sound to play when the turnstile is bumped from the wrong side
    /// </summary>
    [DataField]
    public SoundSpecifier? BumpSound;

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

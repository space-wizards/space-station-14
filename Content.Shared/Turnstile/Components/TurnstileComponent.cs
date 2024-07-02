using Content.Shared.Turnstile.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Turnstile.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(TurnstileSystem))]
public sealed partial class TurnstileComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentAdmittedMob;

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

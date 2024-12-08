using Content.Shared.Smoking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MatchstickComponent : Component
{
    /// <summary>
    ///     Current state to matchstick. Can be <code>Unlit</code>, <code>Lit</code> or <code>Burnt</code>.
    /// </summary>
    [DataField("state")]
    [AutoNetworkedField]
    public SmokableState CurrentState = SmokableState.Unlit;

    /// <summary>
    ///     How long will matchstick last in seconds.
    /// </summary>
    [DataField("duration")]
    public int Duration = 10;

    /// <summary>
    ///     The time that the match will burn out. If null, that means the match is unlit.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? TimeMatchWillBurnOut = null;

    /// <summary>
    ///     Sound played when you ignite the matchstick.
    /// </summary>
    [DataField("igniteSound", required: true)] public SoundSpecifier IgniteSound = default!;
}

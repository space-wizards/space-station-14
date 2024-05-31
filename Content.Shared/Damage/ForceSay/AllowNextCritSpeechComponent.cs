using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.ForceSay;

/// <summary>
///     The reason for this component's existence is slightly unintuitive, so for context: this is put on an entity
///     to allow its next speech attempt to bypass <see cref="MobStateComponent"/> checks. The reason for this is to allow
///     'force saying'--for instance, with deathgasping or with <see cref="DamageForceSayComponent"/>.
///
///     This component is either removed in the <see cref="MobStateSystem"/> speech attempt check, or after <see cref="Timeout"/>
///     has passed. This is to allow a player-submitted forced message in the case of <see cref="DamageForceSayComponent"/>,
///     while also ensuring that it isn't valid forever. It has to work this way, because the server is not a keylogger and doesn't
///     have any knowledge of what the client might actually have typed, so it gives them some leeway for ping.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AllowNextCritSpeechComponent : Component
{
    /// <summary>
    ///     Should be set when adding the component to specify the time that this should be valid for,
    ///     if it should stay valid for some amount of time.
    /// </summary>
    public TimeSpan? Timeout = null;
}

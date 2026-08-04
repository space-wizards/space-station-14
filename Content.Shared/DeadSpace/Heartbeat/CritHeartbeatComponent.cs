// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Heartbeat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CritHeartbeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier PreCriticalSound =
        new SoundPathSpecifier("/Audio/_DeadSpace/Effects/Heartbeat/singlebeat.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier CriticalSound =
        new SoundPathSpecifier("/Audio/_DeadSpace/Effects/Heartbeat/singlebeat.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier DeathSound =
        new SoundPathSpecifier("/Audio/_DeadSpace/Effects/Heartbeat/death_sound.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier EarRingingSound =
        new SoundPathSpecifier("/Audio/_DeadSpace/Effects/Heartbeat/ear_ringing.ogg");
}

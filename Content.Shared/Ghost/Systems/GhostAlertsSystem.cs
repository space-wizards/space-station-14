using Content.Shared.Alert;
using Content.Shared.Ghost.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Systems;

public sealed partial class GhostAlertSystem : EntitySystem
{
    [Dependency] private AlertTeleportSystem _alertTeleportSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    public void MakeGhostAlert(EntityUid ent, ProtoId<AlertPrototype> alert, TimeSpan cooldown, SoundSpecifier? sound = null)
    {
        var query = EntityQueryEnumerator<GhostAlertsComponent, AlertTeleportComponent>();
        while (query.MoveNext(out var uid, out var _, out var alertTeleport))
        {
            _alertTeleportSystem.AddAlertTeleport(uid, ent, alert, cooldown);
            _audioSystem.PlayEntity(sound, uid, uid);
        }
    }
}


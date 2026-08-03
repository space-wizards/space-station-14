using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Follower;
using Content.Shared.Ghost.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;
namespace Content.Shared.Teleportation.Systems;

public abstract partial class AlertTeleportSystem : EntitySystem
{
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    [SubscribeLocalEvent]
    private void OnAlertTeleport(Entity<AlertTeleportComponent> ent, ref AlertTeleportEvent arg)
    {
        var data = ent.Comp.Targets[arg.AlertId];

        if (data.Targets == null)
            return;

        data.Queue++;

        // Just go back to the top of the list.
        if (data.Queue >= data.Targets.Count)
            data.Queue = 0;

        if (!TryGetEntity(data.Targets[data.Queue], out var target) || TerminatingOrDeleted(target))
            return;

        var targetCoords = _transform.GetMapCoordinates(target.Value);

        if (targetCoords.MapId == MapId.Nullspace)
            return;

        // It's a struct, baby
        ent.Comp.Targets[arg.AlertId] = data;

        Dirty(ent);

        if (ent.Comp.Orbit)
        {
            _follower.StartFollowingEntity(ent, target.Value);
        }
        else
        {
            _transform.SetMapCoordinates(ent, _transform.GetMapCoordinates(target.Value));
        }
    }

    public void AddAlertTeleport(EntityUid ent, EntityUid target, ProtoId<AlertPrototype> alert, TimeSpan cooldown, AlertTeleportComponent? comp = null)
    {
        if (!Resolve(ent, ref comp))
            return;

        var targetCoords = _transform.GetMapCoordinates(target);

        // Without this, the client will try to create an alert for items from the spawn menu.
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        if (!comp.Targets.ContainsKey(alert))
            comp.Targets.Add(alert, new AlertTeleportData());

        var data = comp.Targets[alert];

        // Is it bad that we clean up unnecessary objects only when needed? I don't think
        if (data.EndTime <= _timing.CurTime)
            data = default;

        if (data.Targets == null)
            data.Targets = new();

        data.Targets.Add(GetNetEntity(target));

        data.EndTime = _timing.CurTime + cooldown;

        comp.Targets[alert] = data;

        Dirty(ent, comp);

        _alerts.ShowAlert(ent, alert, cooldown: (_timing.CurTime, _timing.CurTime + cooldown), autoRemove: true, showCooldown: false);
    }

    /// <summary>
    /// Gives teleport alert to all entities with a specific component
    /// </summary>
    /// <typeparam name="T">An additional component for the entity filter</typeparam>
    /// <param name="target">The target that the entity will teleport to when the alert is pressed</param>
    /// <param name="alert">The alert that the entity will receive</param>
    /// <param name="cooldown">Alert lifetime</param>
    /// <param name="sound">The sound that the entities will receive when the alert is received</param>
    public void MakeTeleportAlert<T>(EntityUid target, ProtoId<AlertPrototype> alert, TimeSpan cooldown, SoundSpecifier? sound = null) where T : Component
    {
        var query = EntityQueryEnumerator<T, AlertTeleportComponent>();
        while (query.MoveNext(out var uid, out var _, out var alertTeleport))
        {
            AddAlertTeleport(uid, target, alert, cooldown);
            _audioSystem.PlayEntity(sound, uid, uid);
        }
    }
}

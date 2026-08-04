using Content.Shared.Alert;
using Content.Shared.Follower;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class AlertTeleportSystem : EntitySystem
{
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    [SubscribeLocalEvent]
    private void OnAlertTeleport(Entity<AlertTeleportComponent> ent, ref AlertTeleportEvent args)
    {
        var data = ent.Comp.Targets[args.AlertId];

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
        ent.Comp.Targets[args.AlertId] = data;

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

    [SubscribeLocalEvent]
    private void OnClearAlertEvent(Entity<AlertTeleportComponent> ent, ref ClearAlertEvent args)
    {
        ent.Comp.Targets[args.AlertId].Targets.Clear();
    }

    /// <summary>
    /// Adds a teleport alert for a specific entity
    /// </summary>
    /// <param name="ent">The entity to which the alert will be added</param>
    /// <param name="target">The target to which the entity will teleport when the alert is pressed</param>
    /// <param name="alert">The alert that the entity will receive</param>
    /// <param name="cooldown">Alert lifetime</param>
    public void AddAlertTeleport(Entity<AlertTeleportComponent> ent, EntityUid target, ProtoId<AlertPrototype> alert, TimeSpan cooldown)
    {
        var targetCoords = _transform.GetMapCoordinates(target);

        // Without this, the client will try to create an alert for items from the spawn menu.
        if (targetCoords.MapId == MapId.Nullspace)
            return;

        var comp = ent.Comp;

        var curTime = _timing.CurTime;
        var endTime = _timing.CurTime + cooldown;

        if (!comp.Targets.ContainsKey(alert))
            comp.Targets.Add(alert, new AlertTeleportData());

        var data = comp.Targets[alert];

        // Is it bad that we clean up unnecessary objects only when needed? I don't think
        if (data.EndTime <= curTime)
            data = default;

        if (data.Targets == null)
            data.Targets = new();

        data.Targets.Add(GetNetEntity(target));

        data.EndTime = endTime;

        comp.Targets[alert] = data;

        Dirty(ent);

        _alerts.ShowAlert(ent.Owner, alert, cooldown: (curTime, endTime), autoRemove: true, showCooldown: false);
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
            AddAlertTeleport((uid, alertTeleport), target, alert, cooldown);
            _audioSystem.PlayEntity(sound, uid, uid);
        }
    }
}

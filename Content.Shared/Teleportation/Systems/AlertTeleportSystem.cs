using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Follower;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;
namespace Content.Shared.Teleportation.Systems;

public sealed partial class AlertTeleportSystem : EntitySystem
{
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertTeleportComponent, AlertTeleportEvent>(OnAlertTeleport);
    }

    private void OnAlertTeleport(Entity<AlertTeleportComponent> ent, ref AlertTeleportEvent arg)
    {
        var data = ent.Comp.Targets[arg.AlertId];

        if (data.Targets == null)
            return;

        data.Queue++;

        if (data.Queue >= data.Targets.Count)
            data.Queue = 0;

        if (!TryGetEntity(data.Targets[data.Queue], out var target) || TerminatingOrDeleted(target))
            return;

        var targetCoords = _transform.GetMapCoordinates(target.Value);

        if (targetCoords.MapId == MapId.Nullspace)
            return;

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

        if (targetCoords.MapId == MapId.Nullspace)
            return;

        if (!comp.Targets.ContainsKey(alert))
            comp.Targets.Add(alert, new AlertTeleportData());

        var data = comp.Targets[alert];

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
}

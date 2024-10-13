using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Suspicion;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Suspicion;

public sealed class SuspicionRadarSystem : EntitySystem
{
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, PingRadarImplantEvent>(OnPing);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState is not (MobState.Dead or MobState.Critical))
            return;

        if (!_playerManager.TryGetSessionByEntity(ev.Target, out var session))
        {
            Log.Error($"Failed to locate player session for radar removal.");
            return;
        }

        RaiseNetworkEvent(new OnSuspicionRadarOverlayToggledEvent(false), session);
    }

    private void OnPing(EntityUid implantUid, SubdermalImplantComponent implant, PingRadarImplantEvent args)
    {
        var players = EntityQueryEnumerator<SuspicionPlayerComponent>();
        var radarInfo = new List<RadarInfo>();
        var uid = implant.ImplantedEntity;
        if (uid == null)
        {
            Log.Error($"Implant {implantUid} has no implanted entity."); // how
            return;
        }

        if (!_playerManager.TryGetSessionByEntity(uid.Value, out var session))
        {
            Log.Error($"Failed to locate player session for radar ping.");
            return;
        }

        var component = Comp<SuspicionRadarComponent>(implantUid);

        RaiseNetworkEvent(new OnSuspicionRadarOverlayToggledEvent(true), session);

        while (players.MoveNext(out var player, out var playerComponent))
        {
            if (player == uid)
                continue;

            if (_mobStateSystem.IsDead(player))
                continue;

            var mind = _mindSystem.GetMind(player);
            if (mind == null)
                continue;

            if (!_roleSystem.MindHasRole<SuspicionRoleComponent>(mind.Value, out var _, out var role))
                continue;

            var roleComp = role.Value.Comp;
            var position = _transformSystem.GetWorldPosition(player);

            var color = roleComp.Role switch
            {
                SuspicionRole.Traitor when component.ShowTraitors => Color.Red,
                SuspicionRole.Detective => Color.Blue,
                _ => Color.Green
            };

            radarInfo.Add(new RadarInfo(color, position));
        }

        RaiseNetworkEvent(new SuspicionRadarOverlayUpdatedEvent(radarInfo.ToArray()), session);
        args.Handled = true;
    }
}

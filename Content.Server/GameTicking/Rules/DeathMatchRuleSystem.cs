using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Shared.Points;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages <see cref="DeathMatchRuleComponent"/>
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem<DeathMatchRuleComponent>
{
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
        SubscribeLocalEvent<DeathMatchRuleComponent, PlayerPointChangedEvent>(OnPointChanged);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            _respawn.AddToTracker(ev.Mob, uid, tracker);
        }
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // YOU SUICIDED OR GOT THROWN INTO LAVA!
            // WHAT A GIANT FUCKING NERD! LAUGH NOW!
            if (ev.Primary is not KillPlayerSource player)
            {
                _point.AdjustPointValue(ev.Entity, -1, uid, point);
                continue;
            }

            _point.AdjustPointValue(player.PlayerId, 1, uid, point);

            if (ev.Assist is KillPlayerSource assist)
                _point.AdjustPointValue(assist.PlayerId, 1, uid, point);
        }
    }

    private void OnPointChanged(EntityUid uid, DeathMatchRuleComponent component, ref PlayerPointChangedEvent args)
    {
        if (component.Victor != null)
            return;

        if (args.Points < component.KillCap)
            return;

        component.Victor = args.Player;
        _roundEnd.EndRound(component.RestartDelay);
    }
}

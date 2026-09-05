using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Zombies;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Zombies;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerZombieRuleSystem : ZombieRuleSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private ZombieSystem _zombie = default!;

    protected override void ActiveTick(EntityUid uid, Shared.GameTicking.Rules.Components.ZombieRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (!component.NextRoundEndCheck.HasValue || component.NextRoundEndCheck > Timing.CurTime)
            return;
        CheckRoundEnd(component);
        component.NextRoundEndCheck = Timing.CurTime + component.EndCheckDelay;
    }

    /// <summary>
    ///     The big kahoona function for checking if the round is gonna end
    /// </summary>
    private void CheckRoundEnd(Shared.GameTicking.Rules.Components.ZombieRuleComponent zombieRuleComponent)
    {
        var healthy = GetHealthyHumans();
        if (healthy.Count == 1) // Only one human left. spooky
            _popup.PopupEntity(Loc.GetString("zombie-alone"), healthy[0], healthy[0]);

        if (GetInfectedFraction(false) > zombieRuleComponent.ZombieShuttleCallPercentage && !_roundEnd.IsRoundEndRequested())
        {
            foreach (var station in Station.GetStations())
            {
                _chat.DispatchStationAnnouncement(station, Loc.GetString("zombie-shuttle-call"), colorOverride: Color.Crimson);
            }

            _roundEnd.DoRoundEndBehavior(zombieRuleComponent.ZombieRoundEndBehavior, zombieRuleComponent.ZombieEvacShuttleTime);
        }

        // we include dead for this count because we don't want to end the round
        // when everyone gets on the shuttle.
        if (GetInfectedFraction() >= 1) // Oops, all zombies
            _roundEnd.EndRound();
    }

    [SubscribeLocalEvent]
    private void OnZombifySelf(EntityUid uid, IncurableZombieComponent component, ZombifySelfActionEvent args)
    {
        _zombie.ZombifyEntity(uid);
        if (component.Action != null)
            Del(component.Action.Value);
    }
}

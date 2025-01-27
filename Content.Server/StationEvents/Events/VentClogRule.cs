using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Announcements.Systems;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClogRule : StationEventSystem<VentClogRuleComponent>
{
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;

    protected override void Added(EntityUid uid, VentClogRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        _announcer.SendAnnouncement(
            _announcer.GetAnnouncementId(args.RuleId),
            Filter.Broadcast(),
            "station-event-vent-clog-announcement",
            null,
            Color.Gold
        );
    }

    protected override void Started(EntityUid uid, VentClogRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var allReagents = PrototypeManager.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => x.ID).ToList();

        // 'Safe random' for chems, excludes chems in the blacklist defined in the component
        allReagents.RemoveAll(r => component.BlacklistedVentChemicals.Any(a => a == r));

        foreach (var (_, transform) in EntityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != chosenStation)
            {
                continue;
            }

            var solution = new Solution();

            if (!RobustRandom.Prob(0.33f))
                continue;

            var pickAny = RobustRandom.Prob(0.05f);
            var reagent = RobustRandom.Pick(pickAny ? allReagents : component.SafeishVentChemicals);

            var weak = component.WeakReagents.Contains(reagent);
            var quantity = weak ? component.WeakReagentQuantity : component.ReagentQuantity;
            solution.AddReagent(reagent, quantity);

            var foamEnt = Spawn("Foam", transform.Coordinates);
            var spreadAmount = weak ? component.WeakSpread : component.Spread;
            _smoke.StartSmoke(foamEnt, solution, component.Time, spreadAmount);
            Audio.PlayPvs(component.Sound, transform.Coordinates);
        }
    }
}

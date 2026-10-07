using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that alter the laws of silicon entities (e.g. cyborgs, AI) on the affected station.
/// </summary>
/// <seealso cref="IonStormRuleComponent"/>
public sealed partial class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private IonStormSystem _ionStorm = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Started(Entity<IonStormRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        while (query.MoveNext(out var borgUid, out var target, out var xform))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation.Value.Owner ||
                !_random.Prob(target.Chance))
                continue;

            _ionStorm.IonStormTarget((borgUid, target));
        }
    }
}

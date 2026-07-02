using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private IonStormSystem _ionStorm = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var target, out var xform))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation ||
                !_random.Prob(target.Chance))
                continue;

            _ionStorm.IonStormTarget((ent, target));
        }
    }
}

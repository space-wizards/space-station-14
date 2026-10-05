using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private readonly IonStormSystem _ionStorm = default!;

    protected override void Started(Entity<IonStormRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<SiliconLawProviderComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var lawEnt, out var lawBound, out var xform, out var target))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation.Value.Owner)
                continue;

            _ionStorm.IonStormTarget((lawEnt, lawBound, target));
        }
    }
}

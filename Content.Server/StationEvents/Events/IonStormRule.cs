using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that alter the laws of silicon entities (e.g. cyborgs, AI) on the affected station.
/// </summary>
/// <seealso cref="IonStormRuleComponent"/>
public sealed partial class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private IonStormSystem _ionStorm = default!;

    protected override void Started(Entity<IonStormRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var borgUid, out var lawBound, out var xform, out var target))
        {
            // only affect law holders on the station
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation.Value.Owner)
                continue;

            _ionStorm.IonStormTarget((borgUid, lawBound, target));
        }
    }
}

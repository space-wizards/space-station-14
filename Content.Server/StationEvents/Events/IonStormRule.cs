// using Content.Server.Silicons.Laws; imp remove
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random; // imp

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    // [Dependency] private readonly IonStormSystem _ionStorm = default!; // imp remove
    [Dependency] private readonly IRobustRandom _random = default!; // imp

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        // begin imp edit, why tf wasnt this all just an event
        // var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        var query = EntityQueryEnumerator<IonStormTargetComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var target, out var xform))
        // end imp edit
        {
            // only affect law holders on the station, and check random chance (imp edit)
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation ||
                !_random.Prob(target.Chance)) // imp
                continue;
            // begin imp edit again
            var ev = new IonStormEvent();
            RaiseLocalEvent(ent, ref ev);
            //     _ionStorm.IonStormTarget((ent, lawBound, target));
        }
    }
}

// imp add
/// <summary>
/// Event raised on an entity with <see cref="IonStormTargetComponent"/> when an ion storm occurs on the attached station.
/// </summary>
[ByRefEvent]
public record struct IonStormEvent(bool Adminlog = true);

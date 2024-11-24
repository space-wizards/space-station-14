using Content.Server.StationEvents.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class GreytideVirusRule : StationEventSystem<GreytideVirusRuleComponent>
{
    [Dependency] private readonly SharedDoorSystem _door = default!;
    protected override void Started(EntityUid uid, GreytideVirusRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var airlockQuery = EntityQueryEnumerator<AirlockComponent, DoorComponent>();
        while (airlockQuery.MoveNext(out var airlockEnt, out var airlock, out var door))
        {
            _door.TryOpenAndBolt(airlockEnt, door);
        }
    }
}

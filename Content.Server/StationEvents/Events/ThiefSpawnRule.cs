using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ninja.Systems;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Event for spawning a Thief. Auto invoke on start round in Suitable game modes, or can be invoked in mid-game.
/// </summary>
public sealed class ThiefSpawnRule : StationEventSystem<ThiefSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, ThiefSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        Log.Error("Thief spawning Event is started");
    }
}

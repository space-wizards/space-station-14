using Content.Server.GameTicking.Rules.Components;
using Content.Server.ImmovableRod;
using Content.Server.StationEvents.Components;
using Content.Shared.Spawners.Components;

namespace Content.Server.StationEvents.Events;

public sealed class ImmovableRodRule : StationEventSystem<ImmovableRodRuleComponent>
{
    [Dependency] private readonly ImmovableRodSystem _immovableRod = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Started(EntityUid uid, ImmovableRodRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        TryFindRandomTile(out _, out _, out _, out var coords);
        var angle = RobustRandom.NextAngle();
        var direction = angle.ToVec();

        var mapCoords = coords.ToMap(EntityManager, _transform).Offset(-direction * 66f);

        var rod = _immovableRod.SpawnAndLaunch("ImmovableRod", mapCoords, direction);
        EnsureComp<TimedDespawnComponent>(rod).Lifetime = 120f;
    }
}
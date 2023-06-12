using Content.Server.GameTicking.Rules.Components;
using Content.Server.ImmovableRod;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

public sealed class ImmovableRodRule : StationEventSystem<ImmovableRodRuleComponent>
{
    [Dependency] private readonly ImmovableRodSystem _immovableRod = default!;

    protected override void Started(EntityUid uid, ImmovableRodRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        TryFindRandomTile(out _, out _, out _, out var coords);
        var angle = RobustRandom.NextAngle();
        var direction = angle.RotateVec(Vector2.One);

        _immovableRod.SpawnAndLaunch("ImmovableRod", coords, direction);
    }
}
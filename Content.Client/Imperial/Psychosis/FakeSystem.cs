using Content.Shared.Imperial.ICCVar;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;
namespace Content.Client.Fake;

public sealed class FakeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private static TimeSpan _next = TimeSpan.FromSeconds(1);
    private static TimeSpan _update = TimeSpan.FromSeconds(1);
    public override void Initialize()
    {
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) != true)
            return;
        if (_timing.CurTime < _next)
            return;
        _next = _timing.CurTime + _update;
        var query = EntityQueryEnumerator<FakeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Delete == TimeSpan.FromSeconds(0))
                comp.Delete = _timing.CurTime + comp.Life;
            if (comp.Delete < _timing.CurTime)
                _entityManager.QueueDeleteEntity(uid);
            var uidthing = _player.LocalPlayer?.ControlledEntity;
            if (uidthing != null)
            {
                Update(uidthing.Value, uid, comp);
            }
        }
    }
    private void Update(EntityUid uid, EntityUid comp, FakeComponent component)
    {
        var transform = Transform(uid);
        var transform2 = Transform(comp);
        var cords1 = transform.MapPosition;
        var cords2 = transform2.MapPosition;
        var coordinatesX1 = cords1.X;
        var coordinatesY1 = cords1.Y;
        var coordinatesX2 = cords2.X;
        var coordinatesY2 = cords2.Y;
        var distanceX = coordinatesX1 - coordinatesX2;
        var distanceY = coordinatesY1 - coordinatesY2;
        if (distanceX < 0)
        {
            distanceX *= -1;
        }
        if (distanceY < 0)
        {
            distanceY *= -1;
        }
        var distance = distanceX + distanceY;
        if (distance <= 3)
        {
            if (_random.Prob(0.3f))
            {
                _entityManager.QueueDeleteEntity(comp);
                Dirty(component);
            }
        }
    }
}

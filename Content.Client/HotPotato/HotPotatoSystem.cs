using Content.Shared.HotPotato;
using Robust.Shared.Random;
using Robust.Shared.Timing;

public sealed class HotPotatoSystem : SharedHotPotatoSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = AllEntityQuery<ActiveHotPotatoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.TargetTime)
                continue;
            comp.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectCooldown);
            Spawn("HotPotatoEffect", Transform(uid).MapPosition.Offset(_random.NextVector2(0.25f)));
        }
    }
}

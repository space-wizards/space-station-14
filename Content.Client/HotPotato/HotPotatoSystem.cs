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

        foreach (var comp in EntityQuery<ActiveHotPotatoComponent>())
        {
            var curTime = _timing.CurTime;
            if (curTime < comp.TargetTime)
                continue;
            comp.TargetTime = curTime + TimeSpan.FromSeconds(comp.EffectCooldown);
            var ent = Spawn("HotPotatoEffect", Transform(comp.Owner).MapPosition.Offset(_random.NextVector2(0.25f)));
        }
    }
}

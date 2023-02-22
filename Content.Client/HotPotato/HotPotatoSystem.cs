using Content.Shared.HotPotato;
using Robust.Shared.Timing;

public sealed class HotPotatoSystem : SharedHotPotatoSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        foreach (var comp in EntityQuery<ActiveHotPotatoComponent>())
        {
            if (_timing.CurTime < comp.TargetTime)
                continue;
            comp.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectCooldown);
            var ent = Spawn("HotPotatoEffect", Transform(comp.Owner).Coordinates);
        }
    }
}

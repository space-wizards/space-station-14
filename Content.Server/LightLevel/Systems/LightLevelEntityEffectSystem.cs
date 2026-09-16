using Content.Server.LightLevel.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Light;
using Robust.Shared.Timing;

namespace Content.Server.LightLevel.Systems;
public sealed partial class LightLevelEntityEffectSystem : EntitySystem
{
    [Dependency] private LightLevelSystem _lightLevelSystem = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffect = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightLevelEntityEffectComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var curTime = _timing.CurTime;

            if (comp.NextEntityEffect > curTime)
                continue;

            comp.NextEntityEffect = curTime + comp.Interval;

            foreach (var condition in comp.Conditions)
            {
                if (!_lightLevelSystem.TryCalculateLightLevel(uid, out var lightLevel))
                    break;

                if (condition.MinLight < lightLevel && condition.MaxLight > lightLevel)
                    _entityEffect.ApplyEffects(uid, condition.Effects, condition.Scale);
            }
        }
    }
}

using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles...
/// </summary>
public sealed class LightBreakVariationPassSystem : VariationPassSystem<LightBreakVariationPassComponent>
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    protected override void ApplyVariation(Entity<LightBreakVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var query = AllEntityQuery<PoweredLightComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!IsMemberOfStation(uid, ref args))
                continue;

            var chance = (float) Random.NextGaussian(ent.Comp.LightBreakChanceAverage, ent.Comp.LightBreakChanceStdDev);

            if (!Random.Prob(Math.Clamp(chance, 0.0f, 1.0f)))
                continue;

            _poweredLight.TryDestroyBulb(uid, comp);
        }
    }
}

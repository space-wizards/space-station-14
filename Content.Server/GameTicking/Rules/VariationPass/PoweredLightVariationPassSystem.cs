using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
/// This handle randomly destroying lights, causing them to flicker endlessly, or replacing their tube/bulb with different variants.
/// </summary>
public sealed class PoweredLightVariationPassSystem : VariationPassSystem<PoweredLightVariationPassComponent>
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    protected override void ApplyVariation(Entity<PoweredLightVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var query = AllEntityQuery<PoweredLightComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!IsMemberOfStation(uid, ref args))
                continue;

            if (Random.Prob(ent.Comp.LightBreakChance))
            {
                var proto = comp.BulbType switch
                {
                    LightBulbType.Tube => ent.Comp.BrokenLightTubePrototype,
                    _ => ent.Comp.BrokenLightBulbPrototype,
                };

                _poweredLight.ReplaceSpawnedPrototype((uid, comp), proto);
                continue;
            }

            if (!Random.Prob(ent.Comp.LightAgingChance))
                continue;

            if (comp.BulbType == LightBulbType.Tube)
            {
                // some aging fluorescents (tubes) start to flicker
                // its also way too annoying right now so we wrap it in another prob lol
                if (Random.Prob(ent.Comp.AgedLightTubeFlickerChance))
                    _poweredLight.ToggleBlinkingLight(uid, comp, true);
                _poweredLight.ReplaceSpawnedPrototype((uid, comp), ent.Comp.AgedLightTubePrototype);
            }
            else
            {
                _poweredLight.ReplaceSpawnedPrototype((uid, comp), ent.Comp.AgedLightBulbPrototype);
            }
        }
    }
}

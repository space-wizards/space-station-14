using Content.Shared.EntityEffects;
using Content.Shared.Weather;
using Content.Shared.Weather.Effects;

namespace Content.Server.Weather.Effects;

public sealed class WeatherEntityEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherEntityEffectComponent, WeatherEntityAffectedEvent>(OnEntityAffected);
    }

    private void OnEntityAffected(Entity<WeatherEntityEffectComponent> ent, ref WeatherEntityAffectedEvent args)
    {
        if (ent.Comp.EffectPrototype is { } protoId)
            _effects.TryApplyEffect(args.Target, protoId, ent.Comp.Scale);
        else if (ent.Comp.Effects is { Length: > 0 })
            _effects.ApplyEffects(args.Target, ent.Comp.Effects, ent.Comp.Scale, ent.Owner);
    }
}


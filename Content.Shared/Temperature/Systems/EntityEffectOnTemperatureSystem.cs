using Content.Shared.EntityEffects;
using Content.Shared.Temperature.Components;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// Handles applying effect effects on <see cref="EntityEffectOnTemperatureComponent"/>'s when their temperature is reached.
/// </summary>
public sealed partial class EntityEffectOnTemperatureSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnTemperatureChanged(Entity<EntityEffectOnTemperatureComponent> ent, ref TemperatureChangedEvent args)
    {
        var curTemp = args.CurrentTemperature;

        foreach (var entry in ent.Comp.Entries)
        {
            var minTemp = entry.TemperatureRange.X;
            var maxTemp = entry.TemperatureRange.Y;

            if (curTemp < minTemp || curTemp > maxTemp)
                continue;

            _effects.TryApplyEffects(ent.Owner, entry.Effects, entry.Scale);
        }
    }
}

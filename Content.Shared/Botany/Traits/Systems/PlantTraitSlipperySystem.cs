using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Examine;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitSlipperyComponent"/>
public sealed partial class PlantTraitSlipperySystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnProduceSpawned(Entity<PlantTraitSlipperyComponent> ent, ref PlantProduceSpawnedEvent args)
    {
        _effects.TryApplyEffect(args.Produce, new Slipify());
    }

    [SubscribeLocalEvent]
    private void OnProduceExamined(Entity<ProduceComponent> ent, ref ExaminedEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantTraitSlipperyComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var trait)
            || trait.TraitState is not { } description)
            return;

        using (args.PushGroup(nameof(ProduceComponent)))
        {
            args.PushMarkup(Loc.GetString(description));
        }
    }
}

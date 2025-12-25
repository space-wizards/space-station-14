using Content.Server.Botany.Components;
using Content.Server.Botany.Events;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles the chemicals of a plant.
/// </summary>
public sealed class PlantChemicalsSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantChemicalsComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
    }

    private void OnCrossPollinate(Entity<PlantChemicalsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossChemicals(ref ent.Comp.Chemicals, pollenData.Chemicals);
    }
}

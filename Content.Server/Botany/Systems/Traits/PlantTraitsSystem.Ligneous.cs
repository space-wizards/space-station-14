using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Tools;


namespace Content.Server.Botany.Systems;

/// <summary>
/// The plant becomes ligneous, preventing it from being harvested without special tools
/// </summary>
[DataDefinition]
public sealed partial class TraitLigneous : PlantTrait
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void OnInteractUsing(Entity<PlantTraitsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
            return;

        if (!harvest.ReadyForHarvest)
            return;

        if (_plantHolder.IsDead(ent.Owner))
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
            return;
        }

        // ligneous requires sharp tool.
        if (!HasComp<SharpComponent>(args.Used))
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        _plantHarvest.TryHandleHarvest((ent.Owner, harvest), (ent.Owner, plant), args.User);
        args.Handled = true;
    }

    public override void OnDoHarvest(Entity<PlantTraitsComponent> ent, ref DoHarvestEvent args)
    {
        _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
        args.Cancel();
    }

    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-ligneous");
    }
}

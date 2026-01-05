using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// The plant becomes ligneous, preventing it from being harvested without special tools
/// </summary>
[DataDefinition]
public sealed partial class TraitLigneous : PlantTrait
{
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
            _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
            return;
        }

        // Ligneous requires sharp tool.
        if (!HasComp<SharpComponent>(args.Used))
        {
            _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        _plantHarvest.TryHandleHarvest((ent.Owner, harvest), (ent.Owner, plant), args.User);
        args.Handled = true;
    }

    public override void OnDoHarvest(Entity<PlantTraitsComponent> ent, ref DoHarvestEvent args)
    {
        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
        args.Cancel();
    }

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-ligneous");
    }
}

using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;

namespace Content.Shared.Botany.Traits.Systems;

/// <summary>
/// The plant becomes ligneous, preventing it from being harvested without special tools
/// </summary>
public sealed partial class PlantTraitLigneousSystem : EntitySystem
{
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitLigneousComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantTraitLigneousComponent, DoHarvestEvent>(OnDoHarvest);
    }

    private void OnInteractUsing(Entity<PlantTraitLigneousComponent> ent, ref InteractUsingEvent args)
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

    private void OnDoHarvest(Entity<PlantTraitLigneousComponent> ent, ref DoHarvestEvent args)
    {
        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
        args.Cancel();
    }
}

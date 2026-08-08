using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitLigneousComponent"/>
public sealed partial class PlantTraitLigneousSystem : EntitySystem
{
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitLigneousComponent, DoHarvestEvent>(OnDoHarvest, before: [typeof(PlantHarvestSystem)]);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PlantTraitLigneousComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
            return;

        if (!harvest.ReadyForHarvest)
            return;

        if (_plantHolder.IsDead(ent.Owner))
        {
            _popup.PopupCursor(Loc.GetString("plant-component-dead-plant-message"), args.User);
            return;
        }

        // Ligneous requires sharp tool.
        var harvestToolQuality = ent.Comp.HarvestToolQuality;
        if (harvestToolQuality.HasValue && !_tool.HasQuality(args.Used, harvestToolQuality.Value))
        {
            _popup.PopupCursor(Loc.GetString("plant-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        _plantHarvest.TryHandleHarvest(ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnDoHarvest(Entity<PlantTraitLigneousComponent> ent, ref DoHarvestEvent args)
    {
        _popup.PopupCursor(Loc.GetString("plant-component-ligneous-cant-harvest-message"), args.User);
        args.Cancel();
    }
}

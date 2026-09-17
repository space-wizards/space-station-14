using Content.Shared.Administration.Logs;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Manages harvest readiness and execution for plants, including repeat/self-harvest
/// logic and produce spawning, responding to growth and interaction events.
/// </summary>
public sealed partial class PlantHarvestSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;
    [Dependency] private EntityQuery<PlantHarvestComponent> _harvestQuery;
    [Dependency] private EntityQuery<PlantComponent> _plantQuery;
    [Dependency] private EntityQuery<PlantDataComponent> _dataQuery;

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<PlantTrayComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // TODO: Remove this once trays have a proper UI.
        if (!_plantTray.TryGetAlivePlant(ent.AsNullable(), out var plantUid))
            return;

        if (TryHandleHarvest(plantUid.Value, args.User))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantHolderComponent> ent, ref PlantGrowEvent args)
    {
        if (!_harvestQuery.TryComp(ent.Owner, out var harvest)
            || !_plantQuery.TryComp(ent.Owner, out var plant))
            return;

        // If the plant is not mature, set the last harvest to the current age.
        if (ent.Comp.Age < plant.Maturation)
            ent.Comp.LastHarvest = ent.Comp.Age;

        TryAutoHarvest((ent, harvest), ent.Owner);

        // Update whether the plant is ready for harvest.
        var timeLastHarvest = ent.Comp.Age - ent.Comp.LastHarvest;
        if (timeLastHarvest > plant.Production && !ent.Comp.ReadyForHarvest)
        {
            ent.Comp.ReadyForHarvest = true;
            ent.Comp.LastHarvest = ent.Comp.Age;
        }

        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<PlantHolderComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (_plantHolder.IsDead(ent.Owner))
        {
            args.Handled = true;
            _plant.RemovePlant(ent.Owner);
            return;
        }

        if (TryHandleHarvest(ent, args.User))
            args.Handled = true;
    }

    private void TryAutoHarvest(Entity<PlantHarvestComponent> ent, EntityUid user)
    {
        if (ent.Comp.HarvestRepeat != HarvestType.SelfHarvest)
            return;

        if (_dataQuery.TryComp(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.Name):seed} at Pos:{Transform(ent).Coordinates}.");

        DoHarvest(ent.Owner, user);
    }

    /// <summary>
    /// Attempts to harvest the plant, raising <see cref="PlantHarvestAttemptEvent"/> and harvesting if not cancelled.
    /// </summary>
    /// <returns>True if the harvest was handled, even if cancelled.</returns>
    [PublicAPI]
    public bool TryHandleHarvest(EntityUid plant, EntityUid user)
    {
        if (!_holderQuery.TryComp(plant, out var holder) || !holder.ReadyForHarvest)
            return false;

        var ev = new PlantHarvestAttemptEvent(user, plant);
        RaiseLocalEvent(plant, ref ev);
        if (ev.Cancelled)
            return true;

        if (_dataQuery.TryComp(plant, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"{ToPrettyString(user):player} harvested {Loc.GetString(plantData.Name):seed} at Pos:{Transform(user).Coordinates}.");

        DoHarvest(plant, user);
        return true;
    }

    /// <summary>
    /// Harvests the plant and produces the produce.
    /// </summary>
    [PublicAPI]
    public void DoHarvest(Entity<PlantHolderComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!_plantQuery.TryComp(ent.Owner, out var plant)
            || !_dataQuery.TryComp(ent.Owner, out var plantData)
            || !_harvestQuery.TryComp(ent.Owner, out var harvest))
            return;

        if (!ent.Comp.ReadyForHarvest || plantData.ProductPrototypes.Count == 0 || plant.Yield == 0)
            return;

        var name = Loc.GetString(plantData.Name);
        _popup.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        var totalYield = 0;
        if (plant.Yield >= 0)
        {
            totalYield = ent.Comp.YieldMod < 0 ? plant.Yield : plant.Yield * ent.Comp.YieldMod;
            totalYield = Math.Max(1, totalYield);
        }

        var position = Transform(user).Coordinates;
        _botany.SpawnProduce(ent.Owner, position, totalYield);

        ent.Comp.ReadyForHarvest = false;
        ent.Comp.LastHarvest = ent.Comp.Age;
        Dirty(ent);

        if (harvest.HarvestRepeat == HarvestType.NoRepeat)
            _plant.RemovePlant(ent.Owner);

        var ev = new PlantHarvestedEvent(user, ent.Owner);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    /// <summary>
    /// Resets harvest progress to the plant's current age.
    /// </summary>
    [PublicAPI]
    public void ResetHarvestProgress(Entity<PlantHolderComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.LastHarvest = ent.Comp.Age;
        DirtyField(ent, nameof(ent.Comp.LastHarvest));
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    [PublicAPI]
    public void AffectGrowth(Entity<PlantHolderComponent?> ent, int amount)
    {
        if (amount == 0)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!_plantQuery.TryComp(ent.Owner, out var plant))
            return;

        if (amount > 0)
        {
            if (ent.Comp.Age < plant.Maturation)
                _plantHolder.AdjustsAge(ent.Owner, amount);
            else if (!ent.Comp.ReadyForHarvest && plant.Yield <= 0f)
                ent.Comp.LastHarvest -= amount;
        }
        else
        {
            if (ent.Comp.Age < plant.Maturation)
                _plantHolder.AdjustsSkipAging(ent.Owner, 1);
            else if (!ent.Comp.ReadyForHarvest && plant.Yield <= 0f)
                ent.Comp.LastHarvest += amount;
        }

        DirtyField(ent, nameof(ent.Comp.LastHarvest));
    }

    /// <summary>
    /// Changes the harvest repeat of a plant.
    /// </summary>
    [PublicAPI]
    public void ChangeHarvestRepeat(Entity<PlantHarvestComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.HarvestRepeat = ent.Comp.HarvestRepeat switch
        {
            HarvestType.NoRepeat => HarvestType.Repeat,
            HarvestType.Repeat => HarvestType.SelfHarvest,
            _ => ent.Comp.HarvestRepeat,
        };

        DirtyField(ent, nameof(ent.Comp.HarvestRepeat));
    }
}

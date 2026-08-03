using JetBrains.Annotations;
using Content.Shared.Administration.Logs;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;

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

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref PlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder)
            || !TryComp<PlantComponent>(ent.Owner, out var plant))
            return;

        // If the plant is not mature, set the last harvest to the current age.
        if (holder.Age < plant.Maturation)
            ent.Comp.LastHarvest = holder.Age;

        TryAutoHarvest(ent, ent.Owner);

        // Update whether the plant is ready for harvest.
        var timeLastHarvest = holder.Age - ent.Comp.LastHarvest;
        if (timeLastHarvest > plant.Production && !ent.Comp.ReadyForHarvest)
        {
            ent.Comp.ReadyForHarvest = true;
            ent.Comp.LastHarvest = holder.Age;
        }

        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<PlantHarvestComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (_plantHolder.IsDead(ent.Owner))
        {
            args.Handled = true;
            _plant.RemovePlant(ent.Owner);
            return;
        }

        if (!ent.Comp.ReadyForHarvest)
            return;

        var ev = new DoHarvestEvent(args.User, ent.Owner);
        RaiseLocalEvent(ent.Owner, ref ev);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnHandledDoHarvest(Entity<PlantHarvestComponent> ent, ref DoHarvestEvent args)
    {
        if (args.Cancelled)
            return;

        TryHandleHarvest(ent, args.User);
    }

    private void TryAutoHarvest(Entity<PlantHarvestComponent> ent, EntityUid user)
    {
        if (ent.Comp.HarvestRepeat != HarvestType.SelfHarvest)
            return;

        if (TryComp<PlantDataComponent>(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.Name):seed} at Pos:{Transform(ent.Owner).Coordinates}.");

        DoHarvest(ent.AsNullable(), user);
    }

    /// <summary>
    /// Handles harvesting a plant for the specified user.
    /// </summary>
    [PublicAPI]
    public void TryHandleHarvest(EntityUid plant, EntityUid user)
    {
        if (TryComp<PlantDataComponent>(plant, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.Name):seed} at Pos:{Transform(plant).Coordinates}.");

        DoHarvest(plant, user);
    }

    /// <summary>
    /// Harvests the plant and produces the produce.
    /// </summary>
    [PublicAPI]
    public void DoHarvest(Entity<PlantHarvestComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantDataComponent>(ent.Owner, out var plantData)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        if (!ent.Comp.ReadyForHarvest || plantData.ProductPrototypes.Count == 0 || plant.Yield == 0)
            return;

        var name = Loc.GetString(plantData.Name);
        _popup.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        var totalYield = 0;
        if (plant.Yield >= 0)
        {
            totalYield = holder.YieldMod < 0 ? plant.Yield : plant.Yield * holder.YieldMod;
            totalYield = Math.Max(1, totalYield);
        }

        var position = Transform(ent.Owner).Coordinates;
        for (var i = 0; i < totalYield; i++)
        {
            _botany.SpawnProduce(ent.Owner, position);
        }

        ent.Comp.ReadyForHarvest = false;
        ent.Comp.LastHarvest = holder.Age;
        Dirty(ent);

        if (ent.Comp.HarvestRepeat == HarvestType.NoRepeat)
            _plant.RemovePlant(ent.Owner);

        var ev = new AfterDoHarvestEvent(user, ent.Owner);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    [PublicAPI]
    public void AffectGrowth(Entity<PlantHarvestComponent?> ent, int amount)
    {
        if (amount == 0)
            return;

        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder)
            || !TryComp<PlantComponent>(ent.Owner, out var plant))
            return;

        if (amount > 0)
        {
            if (holder.Age < plant.Maturation)
                _plantHolder.AdjustsAge(ent.Owner, amount);
            else if (!ent.Comp.ReadyForHarvest && plant.Yield <= 0f)
                ent.Comp.LastHarvest -= amount;
        }
        else
        {
            if (holder.Age < plant.Maturation)
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

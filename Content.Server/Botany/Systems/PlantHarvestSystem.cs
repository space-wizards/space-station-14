using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Botany;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Manages harvest readiness and execution for plants, including repeat/self-harvest
/// logic and produce spawning, responding to growth and interaction events.
/// </summary>
public sealed class PlantHarvestSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHarvestComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlantHarvestComponent, DoHarvestEvent>(OnHandledDoHarvest);
    }

    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder)
            || !TryComp<PlantComponent>(ent.Owner, out var plant))
            return;

        // Check if plant is ready for harvest.
        var timeLastHarvest = holder.Age - ent.Comp.LastHarvest;
        if (timeLastHarvest > plant.Production && !ent.Comp.ReadyForHarvest)
        {
            ent.Comp.ReadyForHarvest = true;
            ent.Comp.LastHarvest = holder.Age;
            _plant.UpdateSprite(ent.Owner);
            TryAutoHarvest(ent, (ent.Owner, plant), ent.Owner);
        }
    }

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

    private void OnHandledDoHarvest(Entity<PlantHarvestComponent> ent, ref DoHarvestEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PlantComponent>(ent.Owner, out var plant))
            return;

        TryHandleHarvest(ent, (ent.Owner, plant), args.User);
    }

    private void TryAutoHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantComponent> plantEnt, EntityUid user)
    {
        if (ent.Comp.HarvestRepeat != HarvestType.SelfHarvest)
            return;

        if (TryComp<PlantDataComponent>(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(plantEnt.Owner).Coordinates}.");

        DoHarvest(ent, plantEnt, user);
    }

    public void TryHandleHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantComponent> plantEnt, EntityUid user)
    {
        if (TryComp<PlantDataComponent>(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(plantEnt.Owner).Coordinates}.");

        DoHarvest(ent, plantEnt, user);
    }

    /// <summary>
    /// Harvests the plant and produces the produce.
    /// </summary>
    /// <param name="ent">The plant harvest component.</param>
    /// <param name="plantEnt">The plant component.</param>
    /// <param name="user">The user who is harvesting the plant.</param>
    [PublicAPI]
    public void DoHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantComponent> plantEnt, EntityUid user)
    {
        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantDataComponent>(ent.Owner, out var plantData)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        if (!ent.Comp.ReadyForHarvest || plantData.ProductPrototypes.Count == 0 || plant.Yield == 0)
            return;

        var name = Loc.GetString(plantData.DisplayName);
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
            _botany.SpawnProduce((ent.Owner, plantData, plant), position);
        }

        ent.Comp.ReadyForHarvest = false;
        ent.Comp.LastHarvest = holder.Age;

        if (ent.Comp.HarvestRepeat == HarvestType.NoRepeat)
            _plant.RemovePlant(plantEnt.AsNullable());

        _plant.UpdateSprite(plantEnt.AsNullable());
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
    }
}

[ByRefEvent]
public sealed class DoHarvestEvent(EntityUid user, EntityUid target) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}

[ByRefEvent]
public sealed class AfterDoHarvestEvent(EntityUid user, EntityUid target) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}

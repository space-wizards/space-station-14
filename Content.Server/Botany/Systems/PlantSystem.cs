using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles plant behavior and growth processing.
/// </summary>
public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <summary>
    /// Tag for items that can be used to take a sample of a plant.
    /// </summary>
    private static readonly ProtoId<TagPrototype> PlantSampleTakerTag = "PlantSampleTaker";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlantComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<PlantComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlantComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnMapInit(Entity<PlantComponent> ent, ref MapInitEvent args)
    {
        _tray.PlantingPlant(ent.Owner, ent.AsNullable());
    }

    private void OnCrossPollinate(Entity<PlantComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossInt(ref ent.Comp.Yield, pollenData.Yield);
        _mutation.CrossInt(ref ent.Comp.GrowthStages, pollenData.GrowthStages);
        _mutation.CrossFloat(ref ent.Comp.Endurance, pollenData.Endurance);
        _mutation.CrossFloat(ref ent.Comp.Lifespan, pollenData.Lifespan);
        _mutation.CrossFloat(ref ent.Comp.Maturation, pollenData.Maturation);
        _mutation.CrossFloat(ref ent.Comp.Production, pollenData.Production);
        _mutation.CrossFloat(ref ent.Comp.Potency, pollenData.Potency);

        // TODO: Move this to a separate system
        if (_botany.TryGetPlantComponent<PlantTraitsComponent>(args.PollenData, args.PollenProtoId, out var pollenTraits)
            && TryComp<PlantTraitsComponent>(ent.Owner, out var traits))
        {
            _mutation.CrossBool(ref traits.Seedless, pollenTraits.Seedless);
            _mutation.CrossBool(ref traits.Ligneous, pollenTraits.Ligneous);
            _mutation.CrossBool(ref traits.CanScream, pollenTraits.CanScream);
            _mutation.CrossBool(ref traits.TurnIntoKudzu, pollenTraits.TurnIntoKudzu);
        }
    }

    private void OnPlantGrow(Entity<PlantComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, component) = ent;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        // Check if plant is too old.
        if (holder.Age > component.Lifespan)
            _plantHolder.AdjustsHealth(plantUid, -_random.Next(3, 5));
    }

    private void OnExamined(Entity<PlantComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (uid, plant) = ent;

        if (!TryComp<PlantHolderComponent>(uid, out var holder)
            || !TryComp<PlantDataComponent>(uid, out var plantData))
            return;

        using (args.PushGroup(nameof(PlantComponent)))
        {
            var displayName = Loc.GetString(plantData.DisplayName);
            args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                ("seedName", displayName),
                ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

            if (holder.Dead)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-message"));
                return;
            }

            if (holder.Health <= plant.Endurance / 2f)
            {
                args.PushMarkup(Loc.GetString(
                    "plant-holder-component-something-already-growing-low-health-message",
                    ("healthState",
                        Loc.GetString(holder.Age > plant.Lifespan
                            ? "plant-holder-component-plant-old-adjective"
                            : "plant-holder-component-plant-unhealthy-adjective"))));
            }

            if (TryComp<PlantTraitsComponent>(uid, out var traits))
            {
                if (traits.Ligneous)
                    args.PushMarkup(Loc.GetString("mutation-plant-ligneous"));

                if (traits.TurnIntoKudzu)
                    args.PushMarkup(Loc.GetString("mutation-plant-kudzu"));

                if (traits.CanScream)
                    args.PushMarkup(Loc.GetString("mutation-plant-scream"));

                if (!traits.Viable)
                    args.PushMarkup(Loc.GetString("mutation-plant-unviable"));
            }
        }
    }

    private void OnInteractUsing(Entity<PlantComponent> ent, ref InteractUsingEvent args)
    {
        var (plantUid, plant) = ent;

        if (args.Handled)
            return;

        if (!_tag.HasTag(args.Used, PlantSampleTakerTag))
            return;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder)
            || !TryComp<PlantDataComponent>(plantUid, out var plantData))
            return;

        args.Handled = true;

        if (holder.Sampled)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
            return;
        }

        if (holder.Dead)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
            return;
        }

        // Prevent early sampling.
        var maturation = Math.Max(plant.Maturation, 1f);
        var growthStage = Math.Max(1, (int)(holder.Age * plant.GrowthStages / maturation));
        if (growthStage <= 1)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
            return;
        }

        // Damage the plant and produce a seed packet snapshot.
        _plantHolder.AdjustsHealth(plantUid, -_random.Next(3, 5) * 10);

        float? healthOverride;
        if (TryComp<PlantHarvestComponent>(plantUid, out var harvest) && harvest.ReadyForHarvest)
            healthOverride = null;
        else
            healthOverride = holder.Health;

        var seed = _botany.SpawnSeedPacketFromPlant(plantUid, Transform(args.User).Coordinates, args.User, healthOverride);
        _randomHelper.RandomOffset(seed, 0.25f);

        var displayName = Loc.GetString(plantData.DisplayName);
        _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
            ("seedName", displayName)), args.User);

        if (_random.Prob(0.3f))
            holder.Sampled = true;

        ForceUpdateByExternalCause(ent.AsNullable());
    }

    private void Mutate(Entity<PlantComponent?> ent, float severity)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        _mutation.MutatePlant(ent, severity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;

            plantHolder.NextUpdate = _gameTiming.CurTime;
            Update(uid);
        }
    }

    public void Update(Entity<PlantComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<PlantHolderComponent>(uid, out var plantHolder))
            return;

        var curTime = _gameTiming.CurTime;

        // ForceUpdate is used for external triggers like swabbing
        if (plantHolder.ForceUpdate)
            plantHolder.ForceUpdate = false;
        else if (curTime < plantHolder.LastCycle + plantHolder.CycleDelay)
            return;

        plantHolder.LastCycle = curTime;

        if (plantHolder.Dead)
            return;

        TryGetTray(ent, out var trayEnt);
        var plantGrow = new OnPlantGrowEvent(trayEnt);
        RaiseLocalEvent(trayEnt.Owner, ref plantGrow);
        RaiseLocalEvent(uid, ref plantGrow);

        // Process mutations.
        if (plantHolder.MutationLevel > 0)
        {
            Mutate(ent, Math.Min(plantHolder.MutationLevel, 25));
            plantHolder.MutationLevel = 0;
        }

        if (plantHolder.Health <= 0)
            _plantHolder.Die(uid);
    }

    /// <summary>
    /// Forces an update of the tray by external cause.
    /// </summary>
    [PublicAPI]
    public void ForceUpdateByExternalCause(Entity<PlantComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<PlantHolderComponent>(uid, out var plantHolder))
            return;

        plantHolder.SkipAging++;
        plantHolder.ForceUpdate = true;
        Update(ent);
    }

    /// <summary>
    /// Tries to get the tray entity that the plant is in.
    /// </summary>
    [PublicAPI]
    public bool TryGetTray(
        Entity<PlantComponent?> ent,
        [NotNullWhen(true)] out Entity<PlantTrayComponent?> trayEnt
    )
    {
        trayEnt = default!;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        trayEnt.Owner = Transform(ent.Owner).ParentUid;
        if (!TryComp(trayEnt.Owner, out trayEnt.Comp))
            return false;

        return true;
    }

    /// <summary>
    /// Adjusts the potency of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustPotency(Entity<PlantComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Potency = Math.Max(ent.Comp.Potency + amount, 1);
    }

    /// <summary>
    /// Removes the plant from the tray.
    /// </summary>
    [PublicAPI]
    public void RemovePlant(Entity<PlantComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        QueueDel(uid);

        // Delete the plant from the tray.
        if (TryGetTray(ent, out var trayEnt))
            trayEnt.Comp!.PlantEntity = null;
    }

    /// <summary>
    /// Updates the sprite of the plant.
    /// </summary>
    [PublicAPI]
    public void UpdateSprite(Entity<PlantComponent?> ent)
    {
        var (uid, component) = ent;

        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<PlantHolderComponent>(uid, out var plantHolder)
            || !TryComp<PlantDataComponent>(uid, out var plantData)
            || !TryComp<PlantHarvestComponent>(uid, out var harvest))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        _appearance.SetData(uid, PlantVisuals.PlantRsi, plantData.PlantRsi.ToString(), app);

        if (plantHolder.Dead)
        {
            _appearance.SetData(uid, PlantVisuals.PlantState, "dead", app);
        }
        else if (harvest.ReadyForHarvest)
        {
            _appearance.SetData(uid, PlantVisuals.PlantState, "harvest", app);
        }
        else
        {
            if (plantHolder.Age < component.Maturation)
            {
                var growthStage = Math.Max(1, (int)(plantHolder.Age * component.GrowthStages / component.Maturation));
                _appearance.SetData(uid, PlantVisuals.PlantState, $"stage-{growthStage}", app);
            }
            else
            {
                _appearance.SetData(uid, PlantVisuals.PlantState, $"stage-{component.GrowthStages}", app);
            }
        }

        if (TryGetTray(uid, out var trayEnt))
            _tray.UpdateWarnings(trayEnt);
    }
}

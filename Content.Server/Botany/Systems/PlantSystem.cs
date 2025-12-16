using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles plant behavior and growth processing.
/// </summary>
public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <summary>
    /// Tag for items that can be used to take a sample of a plant.
    /// </summary>
    private static readonly ProtoId<TagPrototype> PlantSampleTakerTag = "PlantSampleTaker";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<PlantComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlantComponent, InteractUsingEvent>(OnInteractUsing);
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
        var (_, tray) = args.Tray;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        // Check if plant is too old.
        if (holder.Age > component.Lifespan)
        {
            holder.Health -= _random.Next(3, 5) * tray.TraySpeedMultiplier;
            if (tray.DrawWarnings)
                tray.UpdateSpriteAfterUpdate = true;
        }
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
        holder.Health -= _random.Next(3, 5) * 10;

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

        var trayUid = Transform(plantUid).ParentUid;
        if (TryComp<PlantTrayComponent>(trayUid, out var tray))
        {
            tray.UpdateSpriteAfterUpdate = true;
            _tray.ForceUpdateByExternalCause(trayUid);
        }
    }

    /// <summary>
    /// Adjusts the potency of a plant component.
    /// </summary>
    [PublicAPI]
    public void AdjustPotency(Entity<PlantComponent> ent, float delta)
    {
        var (_, plant) = ent;

        plant.Potency = Math.Max(plant.Potency + delta, 1);
        Dirty(ent);
    }
}

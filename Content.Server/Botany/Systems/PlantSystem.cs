using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

public sealed class PlantSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlantComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantComponent, InteractHandEvent>(OnInteractHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantComponent>();
        while (query.MoveNext(out var uid, out var plant))
        {
            if (plant.NextUpdate > _gameTiming.CurTime)
                continue;
            plant.NextUpdate = _gameTiming.CurTime + plant.UpdateDelay;

            Update(uid, plant);
        }
    }

    private void OnExamine(Entity<PlantComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var plant = entity.Comp;

        using (args.PushGroup(nameof(PlantComponent)))
        {
            var (_, component) = entity;
            SeedData? seed = plant.Seed;
            if (seed == null)
                return;

            if (!plant.Dead)
            {
                var displayName = Loc.GetString(seed.DisplayName);
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                    ("seedName", displayName),
                    ("toBeForm", displayName.EndsWith('s') ? "are" : "is")));

                if (plant.Health <= seed.Endurance / 2)
                {
                    args.PushMarkup(Loc.GetString(
                        "plant-holder-component-something-already-growing-low-health-message",
                        ("healthState",
                            Loc.GetString(plant.Age > seed.Lifespan
                                ? "plant-holder-component-plant-old-adjective"
                                : "plant-holder-component-plant-unhealthy-adjective"))));
                }
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }
        }
    }

    private void OnInteractUsing(Entity<PlantComponent> entity, ref InteractUsingEvent args)
    {
        GetEverything(entity, out var plant, out var seed, out var holder);

        if (_tagSystem.HasTag(args.Used, "PlantSampleTaker"))
        {
            args.Handled = true;
            if (plant == null || seed == null)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), args.User);
                return;
            }

            if (plant.Sampled)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), args.User);
                return;
            }

            if (plant.Dead)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), args.User);
                return;
            }

            if (GetCurrentGrowthStage(entity) <= 1)
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
                return;
            }

            plant.Health -= (_random.Next(3, 5) * 10);

            float? healthOverride;
            if (plant.Harvest)
            {
                healthOverride = null;
            }
            else
            {
                healthOverride = plant.Health;
            }
            var packetSeed = seed;
            var seedItem = _botany.SpawnSeedPacket(packetSeed, Transform(args.User).Coordinates, args.User, healthOverride);
            _randomHelper.RandomOffset(seedItem, 0.25f);
            var displayName = Loc.GetString(seed.DisplayName);
            _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
                ("seedName", displayName)), args.User);

            DoScream(entity, seed);

            if (_random.Prob(0.3f))
                plant.Sampled = true;

            if (plant.PlantHolderUid != null)
                _plantHolder.UpdateSprite(plant.PlantHolderUid.Value, holder);
            return;
        }

        if (HasComp<SharpComponent>(args.Used))
        {
            DoHarvest(entity, args.User);
            args.Handled = true;
        }
    }

    private void OnInteractHand(Entity<PlantComponent> entity, ref InteractHandEvent args)
    {
        DoHarvest(entity, args.User);
    }

    /// <summary>
    /// A player has clicked on a harvestable plant. Make its produce and update tracking.
    /// </summary>
    public bool DoHarvest(EntityUid plantEntity, EntityUid user)
    {
        GetEverything(plantEntity, out var plant, out var seed, out var holder);
        if (plant == null || seed == null || holder == null)
            return false;

        if (seed == null || Deleted(user))
            return false;

        if (plant.Harvest && !plant.Dead)
        {
            if (TryComp<HandsComponent>(user, out var hands))
            {
                if (!_botany.CanHarvest(seed, hands.ActiveHandEntity))
                {
                    return false;
                }
            }
            else if (!_botany.CanHarvest(seed))
            {
                return false;
            }

            _botany.Harvest(seed, user);
            AfterHarvest(plantEntity, plant);
            return true;
        }
        if (!plant.Dead)
            return false;

        RemovePlant(plantEntity);
        AfterHarvest(plantEntity, plant);
        return true;
    }

    /// <summary>
    /// Delete this plant and remove it from its plantHolder.
    /// </summary>
    public void RemovePlant(EntityUid uid)
    {
        GetEverything(uid, out var plant, out _, out var holder);

        QueueDel(uid);
        if (holder != null && plant != null && plant.PlantHolderUid != null)
        {
            holder.PlantUid = null;
            _plantHolder.ForceUpdateByExternalCause(plant.PlantHolderUid.Value);
        }
    }

    private void AfterHarvest(EntityUid uid, PlantComponent? component = null)
    {
        GetEverything(uid, out var plant, out var seed, out var holder);

        if (plant == null || seed == null || holder == null)
            return;

        plant.Harvest = false;
        plant.LastProduce = plant.Age;

        DoScream(uid, seed);

        if (seed.HarvestRepeat == HarvestType.NoRepeat)
        {
            holder.PlantUid = null;
            RemovePlant(uid);
        }
        else
            UpdateSprite(uid, plant);
    }

    /// <summary>
    /// The plant is harvesting itself on a growth tick.
    /// </summary>
    public void AutoHarvest(EntityUid uid, PlantComponent? component = null, SeedComponent? seed = null)
    {
        if (!Resolve(uid, ref component, ref seed))
            return;

        var seedData = seed.Seed;

        if (seedData == null || !component.Harvest)
            return;

        _botany.AutoHarvest(seedData, Transform(uid).Coordinates);
        AfterHarvest(uid, component);
    }

    /// <summary>
    /// AAAAAAAAAAAAAHHHHHHHHHHH!
    /// </summary>
    public bool DoScream(EntityUid plant, SeedData? seed = null)
    {
        if (seed == null || seed.CanScream == false)
            return false;

        _audio.PlayPvs(seed.ScreamSound, plant);
        return true;
    }

    public void Update(EntityUid plantuid, PlantComponent? plant = null)
    {
        if (!GetEverything(plantuid, out plant, out var seed, out var holder) || plant == null || seed == null || holder == null)
            return;

        plant.Health = MathHelper.Clamp(plant.Health, 0, seed.Endurance);
        // Process mutations
        if (plant.MutationLevel > 0)
        {
            Mutate(plantuid, Math.Min(plant.MutationLevel, 25));
            plant.MutationLevel = 0;
        }

        // Advance plant age here.
        if (plant.SkipAging > 0)
            plant.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
                plant.Age += (int)(1 * HydroponicsSpeedMultiplier);
        }

        // Nutrient consumption.
        if (seed.NutrientConsumption > 0 && holder.NutritionLevel > 0 && _random.Prob(0.75f))
        {
            holder.NutritionLevel -= MathF.Max(0f, seed.NutrientConsumption * HydroponicsSpeedMultiplier);
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Water consumption.
        if (seed.WaterConsumption > 0 && holder.WaterLevel > 0 && _random.Prob(0.75f))
        {
            holder.WaterLevel -= MathF.Max(0f,
                seed.WaterConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

        // Make sure genetics are viable.
        if (!seed.Viable)
        {
            AffectGrowth(plantuid, -1, plant);
            plant.Health -= 6 * healthMod;
        }

        // Prevents the plant from aging when lacking resources.
        // Limits the effect on aging so that when resources are added, the plant starts growing in a reasonable amount of time.
        if (plant.SkipAging < 10)
        {
            // Make sure the plant is not starving.
            if (holder.NutritionLevel > 5)
            {
                plant.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth(plantuid, -1, plant);
                plant.Health -= healthMod;
            }

            // Make sure the plant is not thirsty.
            if (holder.WaterLevel > 10)
            {
                plant.Health += Convert.ToInt32(_random.Prob(0.35f)) * healthMod;
            }
            else
            {
                AffectGrowth(plantuid, -1, plant);
                plant.Health -= healthMod;
            }

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        var environment = _atmosphere.GetContainingMixture(plantuid, true, true) ?? GasMixture.SpaceGas;

        holder.MissingGas = 0;
        if (seed.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in seed.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    holder.MissingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (holder.MissingGas > 0)
            {
                plant.Health -= holder.MissingGas * HydroponicsSpeedMultiplier;
                if (holder.DrawWarnings)
                    holder.UpdateSpriteAfterUpdate = true;
            }
        }

        // SeedPrototype pressure resistance.
        var pressure = environment.Pressure;
        if (pressure < seed.LowPressureTolerance || pressure > seed.HighPressureTolerance)
        {
            plant.Health -= healthMod;
            holder.ImproperPressure = true;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            holder.ImproperPressure = false;
        }

        // SeedPrototype ideal temperature.
        if (MathF.Abs(environment.Temperature - seed.IdealHeat) > seed.HeatTolerance)
        {
            plant.Health -= healthMod;
            holder.ImproperHeat = true;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            holder.ImproperHeat = false;
        }

        // Gas production.
        var exudeCount = seed.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in seed.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(amount * MathF.Round(seed.Potency) / exudeCount)));
            }
        }

        // Toxin levels beyond the plant's tolerance cause damage.
        // They are, however, slowly reduced over time.
        if (holder.Toxins > 0)
        {
            var toxinUptake = MathF.Max(1, MathF.Round(holder.Toxins / 10f));
            if (holder.Toxins > seed.ToxinsTolerance)
            {
                plant.Health -= toxinUptake;
            }

            holder.Toxins -= toxinUptake;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Pest levels.
        if (holder.PestLevel > 0)
        {
            if (holder.PestLevel > seed.PestTolerance)
            {
                plant.Health -= HydroponicsSpeedMultiplier;
            }

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        // Weed levels.
        if (holder.WeedLevel > 0)
        {
            if (holder.WeedLevel >= seed.WeedTolerance)
            {
                plant.Health -= HydroponicsSpeedMultiplier;
            }

            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }

        if (plant.Age > seed.Lifespan)
        {
            plant.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else if (plant.Age < 0 && plant.PlantHolderUid != null) // Revert back to seed packet!
        {
            var packetSeed = seed;
            // will put it in the trays hands if it has any, please do not try doing this
            if (plant.PlantHolderUid != null)
                _botany.SpawnSeedPacket(packetSeed, Transform(plant.PlantHolderUid.Value).Coordinates, plant.PlantHolderUid.Value);
            RemovePlant(plantuid);
            holder.ForceUpdate = true;
            if (plant.PlantHolderUid != null)
                _plantHolder.Update(plant.PlantHolderUid.Value, holder);
            return;
        }

        if (plant.Health <= 0)
            Die(plantuid);

        if (plant.Harvest && seed.HarvestRepeat == HarvestType.SelfHarvest)
            AutoHarvest(plantuid, plant);

        // If enough time has passed since the plant was harvested, we're ready to harvest again!
        if (!plant.Dead && seed.ProductPrototypes.Count > 0)
        {
            if (plant.Age > seed.Production)
            {
                if (plant.Age - plant.LastProduce > seed.Production && !plant.Harvest)
                {
                    plant.Harvest = true;
                    plant.LastProduce = plant.Age;
                }
            }
            else
            {
                if (plant.Harvest)
                {
                    plant.Harvest = false;
                    plant.LastProduce = plant.Age;
                }
            }
        }

        UpdateSprite(plantuid, plant);
    }

    /// <summary>
    /// Gets all the components containing data used by the plant: Plant, its SeedData for convenience, and its PlantHolder.
    /// Returns false if any of them weren't found, but components that were present will be available.
    /// </summary>
    public bool GetEverything(EntityUid plantEntity, out PlantComponent? plant, out SeedData? seed, out PlantHolderComponent? holder)
    {
        seed = null;
        holder = null;
        bool plantFound = TryComp<PlantComponent>(plantEntity, out plant);
        bool holderFound = false;
        if (plant != null)
        {
            seed = plant.Seed;
            holderFound = TryComp<PlantHolderComponent>(plant.PlantHolderUid, out holder);
        }
        return plantFound && seed != null && holderFound;
    }

    /// <summary>
    /// Cease the growth and harvesting of a plant through the conclusion of biological processes.
    /// </summary>
    public void Die(EntityUid uid, PlantComponent? plant = null)
    {
        if (!GetEverything(uid, out plant, out var seed, out var holder) || plant == null)
            return;

        plant.Dead = true;
        plant.Harvest = false;
        plant.MutationLevel = 0;
        UpdateSprite(uid, plant);

        if (holder != null)
        {
            holder.ImproperHeat = false;
            holder.ImproperPressure = false;
            holder.WeedLevel = Math.Clamp(holder.WeedLevel + (1 * HydroponicsSpeedMultiplier), 0f, 10f);
            holder.PestLevel = 0;
            if (plant.PlantHolderUid != null)
                _plantHolder.UpdateSprite(plant.PlantHolderUid.Value, holder);
        }
    }

    private int GetCurrentGrowthStage(Entity<PlantComponent> entity)
    {
        GetEverything(entity, out var plant, out _, out _);
        if (plant == null || plant.Seed == null)
            return 0;
        var seed = plant.Seed;

        var result = Math.Max(1, (int)(plant.Age * seed.GrowthStages / seed.Maturation));
        return result;
    }

    /// <summary>
    /// Adjusts the aging of a plant. Positive amounts make plant older (and harvests soon), negative amounts make the plant delay growth
    /// (and slows down harvests)
    /// </summary>
    public void AffectGrowth(EntityUid uid, int amount, PlantComponent? component = null)
    {
        if (!GetEverything(uid, out component, out var seed, out var _) || component == null || seed == null)
            return;

        if (amount > 0)
        {
            if (component.Age < seed.Maturation)
                component.Age += amount;
            else if (!component.Harvest && seed.Yield <= 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < seed.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && seed.Yield <= 0f)
                component.LastProduce += amount;
        }
    }

    private void Mutate(EntityUid uid, float severity)
    {
        GetEverything(uid, out _, out var seed, out _);

        if (seed != null)
        {
            _mutation.CheckRandomMutations(uid, ref seed, severity);
        }
    }

    /// <summary>
    /// Watch your plant grow.
    /// </summary>
    public void UpdateSprite(EntityUid uid, PlantComponent? component = null)
    {
        GetEverything(uid, out var plant, out _, out _);
        if (plant == null)
            return;
        var seed = plant.Seed;
        if (seed == null)
            return;

        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (seed == null)
            return;

        if (component.Dead)
        {
            _appearance.SetData(uid, PlantVisuals.PlantRsi, seed.PlantRsi.ToString(), app);
            _appearance.SetData(uid, PlantVisuals.PlantState, "dead", app);
        }
        else if (component.Harvest)
        {
            _appearance.SetData(uid, PlantVisuals.PlantRsi, seed.PlantRsi.ToString(), app);
            _appearance.SetData(uid, PlantVisuals.PlantState, "harvest", app);
        }
        else if (component.Age < seed.Maturation)
        {
            var growthStage = GetCurrentGrowthStage((uid, component));

            _appearance.SetData(uid, PlantVisuals.PlantRsi, seed.PlantRsi.ToString(), app);
            _appearance.SetData(uid, PlantVisuals.PlantState, $"stage-{growthStage}", app);
            component.LastProduce = component.Age;
        }
        else
        {
            _appearance.SetData(uid, PlantVisuals.PlantRsi, seed.PlantRsi.ToString(), app);
            _appearance.SetData(uid, PlantVisuals.PlantState, $"stage-{seed.GrowthStages}", app);
        }

        Dirty(uid, app);
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Botany;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Medical;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Speech.Components;
using Content.Server.Spreader;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Server.Traits.Assorted;
using Content.Server.Zombies;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects.EffectConditions;
using Content.Shared.EntityEffects.Effects.PlantMetabolism;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

using TemperatureCondition = Content.Shared.EntityEffects.EffectConditions.Temperature; // disambiguate the namespace
using PolymorphEffect = Content.Shared.EntityEffects.Effects.Polymorph;

namespace Content.Server.EntityEffects;

public sealed class EntityEffectSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly NarcolepsySystem _narcolepsy = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CheckEntityEffectConditionEvent<TemperatureCondition>>(OnCheckTemperature);
        SubscribeLocalEvent<CheckEntityEffectConditionEvent<Breathing>>(OnCheckBreathing);
        SubscribeLocalEvent<CheckEntityEffectConditionEvent<OrganType>>(OnCheckOrganType);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustHealth>>(OnExecutePlantAdjustHealth);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustMutationLevel>>(OnExecutePlantAdjustMutationLevel);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustMutationMod>>(OnExecutePlantAdjustMutationMod);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustPests>>(OnExecutePlantAdjustPests);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustPotency>>(OnExecutePlantAdjustPotency);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustToxins>>(OnExecutePlantAdjustToxins);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustWater>>(OnExecutePlantAdjustWater);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAdjustWeeds>>(OnExecutePlantAdjustWeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantAffectGrowth>>(OnExecutePlantAffectGrowth);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantChangeStat>>(OnExecutePlantChangeStat);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantCryoxadone>>(OnExecutePlantCryoxadone);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantDestroySeeds>>(OnExecutePlantDestroySeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantDiethylamine>>(OnExecutePlantDiethylamine);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantPhalanximine>>(OnExecutePlantPhalanximine);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantRestoreSeeds>>(OnExecutePlantRestoreSeeds);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<AdjustTemperature>>(OnExecuteAdjustTemperature);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<AreaReactionEffect>>(OnExecuteAreaReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<CauseZombieInfection>>(OnExecuteCauseZombieInfection);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChemCleanBloodstream>>(OnExecuteChemCleanBloodstream);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChemVomit>>(OnExecuteChemVomit);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<CreateEntityReactionEffect>>(OnExecuteCreateEntityReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<CreateGas>>(OnExecuteCreateGas);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<CureZombieInfection>>(OnExecuteCureZombieInfection);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<Emote>>(OnExecuteEmote);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<EmpReactionEffect>>(OnExecuteEmpReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ExplosionReactionEffect>>(OnExecuteExplosionReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<FlammableReaction>>(OnExecuteFlammableReaction);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<FlashReactionEffect>>(OnExecuteFlashReactionEffect);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<Ignite>>(OnExecuteIgnite);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<MakeSentient>>(OnExecuteMakeSentient);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ModifyBleedAmount>>(OnExecuteModifyBleedAmount);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ModifyBloodLevel>>(OnExecuteModifyBloodLevel);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ModifyLungGas>>(OnExecuteModifyLungGas);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<Oxygenate>>(OnExecuteOxygenate);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantMutateChemicals>>(OnExecutePlantMutateChemicals);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantMutateConsumeGasses>>(OnExecutePlantMutateConsumeGasses);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantMutateExudeGasses>>(OnExecutePlantMutateExudeGasses);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantMutateHarvest>>(OnExecutePlantMutateHarvest);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PlantSpeciesChange>>(OnExecutePlantSpeciesChange);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<PolymorphEffect>>(OnExecutePolymorph);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ResetNarcolepsy>>(OnExecuteResetNarcolepsy);
    }

    private void OnCheckTemperature(ref CheckEntityEffectConditionEvent<TemperatureCondition> args)
    {
        args.Result = false;
        if (TryComp(args.Args.TargetEntity, out TemperatureComponent? temp))
        {
            if (temp.CurrentTemperature > args.Condition.Min && temp.CurrentTemperature < args.Condition.Max)
                args.Result = true;
        }
    }

    private void OnCheckBreathing(ref CheckEntityEffectConditionEvent<Breathing> args)
    {
        if (!TryComp(args.Args.TargetEntity, out RespiratorComponent? respiratorComp))
        {
            args.Result = !args.Condition.IsBreathing;
            return;
        }

        var breathingState = _respirator.IsBreathing((args.Args.TargetEntity, respiratorComp));
        args.Result = args.Condition.IsBreathing == breathingState;
    }

    private void OnCheckOrganType(ref CheckEntityEffectConditionEvent<OrganType> args)
    {
        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.OrganEntity == null)
            {
                args.Result = false;
                return;
            }

            args.Result = OrganCondition(args.Condition, reagentArgs.OrganEntity.Value);
            return;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    public bool OrganCondition(OrganType condition, Entity<MetabolizerComponent?> metabolizer)
    {
        metabolizer.Comp ??= EntityManager.GetComponentOrNull<MetabolizerComponent>(metabolizer.Owner);
        if (metabolizer.Comp != null
            && metabolizer.Comp.MetabolizerTypes != null
            && metabolizer.Comp.MetabolizerTypes.Contains(condition.Type))
            return condition.ShouldHave;
        return !condition.ShouldHave;
    }

    /// <summary>
    ///     Checks if the plant holder can metabolize the reagent or not. Checks if it has an alive plant by default.
    /// </summary>
    /// <param name="plantHolder">The entity holding the plant</param>
    /// <param name="plantHolderComponent">The plant holder component</param>
    /// <param name="entityManager">The entity manager</param>
    /// <param name="mustHaveAlivePlant">Whether to check if it has an alive plant or not</param>
    /// <returns></returns>
    private bool CanMetabolizePlant(EntityUid plantHolder, [NotNullWhen(true)] out PlantHolderComponent? plantHolderComponent,
        bool mustHaveAlivePlant = true, bool mustHaveMutableSeed = false)
    {
        plantHolderComponent = null;

        if (!TryComp(plantHolder, out plantHolderComponent))
            return false;

        if (mustHaveAlivePlant && (plantHolderComponent.Seed == null || plantHolderComponent.Dead))
            return false;

        if (mustHaveMutableSeed && (plantHolderComponent.Seed == null || plantHolderComponent.Seed.Immutable))
            return false;

        return true;
    }

    private void OnExecutePlantAdjustHealth(ref ExecuteEntityEffectEvent<PlantAdjustHealth> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.Health += args.Effect.Amount;
        _plantHolder.CheckHealth(args.Args.TargetEntity, plantHolderComp);
    }

    private void OnExecutePlantAdjustMutationLevel(ref ExecuteEntityEffectEvent<PlantAdjustMutationLevel> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.MutationLevel += args.Effect.Amount * plantHolderComp.MutationMod;
    }

    private void OnExecutePlantAdjustMutationMod(ref ExecuteEntityEffectEvent<PlantAdjustMutationMod> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.MutationMod += args.Effect.Amount;
    }

    private void OnExecutePlantAdjustNutrition(ref ExecuteEntityEffectEvent<PlantAdjustNutrition> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveAlivePlant: false))
            return;

        _plantHolder.AdjustNutrient(args.Args.TargetEntity, args.Effect.Amount, plantHolderComp);
    }

    private void OnExecutePlantAdjustPests(ref ExecuteEntityEffectEvent<PlantAdjustPests> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.PestLevel += args.Effect.Amount;
    }

    private void OnExecutePlantAdjustPotency(ref ExecuteEntityEffectEvent<PlantAdjustPotency> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        if (plantHolderComp.Seed == null)
            return;

        _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
        plantHolderComp.Seed.Potency = Math.Max(plantHolderComp.Seed.Potency + args.Effect.Amount, 1);
    }

    private void OnExecutePlantAdjustToxins(ref ExecuteEntityEffectEvent<PlantAdjustToxins> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.Toxins += args.Effect.Amount;
    }

    private void OnExecutePlantAdjustWater(ref ExecuteEntityEffectEvent<PlantAdjustWater> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveAlivePlant: false))
            return;

        _plantHolder.AdjustWater(args.Args.TargetEntity, args.Effect.Amount, plantHolderComp);
    }

    private void OnExecutePlantAdjustWeeds(ref ExecuteEntityEffectEvent<PlantAdjustWeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        plantHolderComp.WeedLevel += args.Effect.Amount;
    }

    private void OnExecutePlantAffectGrowth(ref ExecuteEntityEffectEvent<PlantAffectGrowth> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        _plantHolder.AffectGrowth(args.Args.TargetEntity, (int) args.Effect.Amount, plantHolderComp);
    }

    // Mutate reference 'val' between 'min' and 'max' by pretending the value
    // is representable by a thermometer code with 'bits' number of bits and
    // randomly flipping some of them.
    private void MutateFloat(ref float val, float min, float max, int bits)
    {
        if (min == max)
        {
            val = min;
            return;
        }

        // Starting number of bits that are high, between 0 and bits.
        // In other words, it's val mapped linearly from range [min, max] to range [0, bits], and then rounded.
        int valInt = (int)MathF.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp.
        valInt = Math.Clamp(valInt, 0, bits);

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasive it it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)valInt / bits;
        int valIntMutated;
        if (_random.Prob(probIncrease))
        {
            valIntMutated = valInt + 1;
        }
        else
        {
            valIntMutated = valInt - 1;
        }

        // Set value based on mutated thermometer code.
        float valMutated = Math.Clamp((float)valIntMutated / bits * (max - min) + min, min, max);
        val = valMutated;
    }

    private void MutateInt(ref int val, int min, int max, int bits)
    {
        if (min == max)
        {
            val = min;
            return;
        }

        // Starting number of bits that are high, between 0 and bits.
        // In other words, it's val mapped linearly from range [min, max] to range [0, bits], and then rounded.
        int valInt = (int)MathF.Round((val - min) / (max - min) * bits);
        // val may be outside the range of min/max due to starting prototype values, so clamp.
        valInt = Math.Clamp(valInt, 0, bits);

        // Probability that the bit flip increases n.
        // The higher the current value is, the lower the probability of increasing value is, and the higher the probability of decreasing it.
        // In other words, it tends to go to the middle.
        float probIncrease = 1 - (float)valInt / bits;
        int valMutated;
        if (_random.Prob(probIncrease))
        {
            valMutated = val + 1;
        }
        else
        {
            valMutated = val - 1;
        }

        valMutated = Math.Clamp(valMutated, min, max);
        val = valMutated;
    }

    private void OnExecutePlantChangeStat(ref ExecuteEntityEffectEvent<PlantChangeStat> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        if (plantHolderComp.Seed == null)
            return;

        var member = plantHolderComp.Seed.GetType().GetField(args.Effect.TargetValue);

        if (member == null)
        {
            _mutation.Log.Error(args.Effect.GetType().Name + " Error: Member " + args.Effect.TargetValue + " not found on " + plantHolderComp.Seed.GetType().Name + ". Did you misspell it?");
            return;
        }

        var currentValObj = member.GetValue(plantHolderComp.Seed);
        if (currentValObj == null)
            return;

        if (member.FieldType == typeof(float))
        {
            var floatVal = (float)currentValObj;
            MutateFloat(ref floatVal, args.Effect.MinValue, args.Effect.MaxValue, args.Effect.Steps);
            member.SetValue(plantHolderComp.Seed, floatVal);
        }
        else if (member.FieldType == typeof(int))
        {
            var intVal = (int)currentValObj;
            MutateInt(ref intVal, (int)args.Effect.MinValue, (int)args.Effect.MaxValue, args.Effect.Steps);
            member.SetValue(plantHolderComp.Seed, intVal);
        }
        else if (member.FieldType == typeof(bool))
        {
            var boolVal = (bool)currentValObj;
            boolVal = !boolVal;
            member.SetValue(plantHolderComp.Seed, boolVal);
        }
    }

    private void OnExecutePlantCryoxadone(ref ExecuteEntityEffectEvent<PlantCryoxadone> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        var deviation = 0;
        var seed = plantHolderComp.Seed;
        if (seed == null)
            return;
        if (plantHolderComp.Age > seed.Maturation)
            deviation = (int) Math.Max(seed.Maturation - 1, plantHolderComp.Age - _random.Next(7, 10));
        else
            deviation = (int) (seed.Maturation / seed.GrowthStages);
        plantHolderComp.Age -= deviation;
        plantHolderComp.LastProduce = plantHolderComp.Age;
        plantHolderComp.SkipAging++;
        plantHolderComp.ForceUpdate = true;
    }

    private void OnExecutePlantDestroySeeds(ref ExecuteEntityEffectEvent<PlantDestroySeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveMutableSeed: true))
            return;

        if (plantHolderComp.Seed!.Seedless == false)
        {
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            _popup.PopupEntity(
                Loc.GetString("botany-plant-seedsdestroyed"),
                args.Args.TargetEntity,
                PopupType.SmallCaution
            );
            plantHolderComp.Seed.Seedless = true;
        }
    }

    private void OnExecutePlantDiethylamine(ref ExecuteEntityEffectEvent<PlantDiethylamine> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveMutableSeed: true))
            return;

        if (_random.Prob(0.1f))
        {
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed!.Lifespan++;
        }

        if (_random.Prob(0.1f))
        {
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed!.Endurance++;
        }
    }

    private void OnExecutePlantPhalanximine(ref ExecuteEntityEffectEvent<PlantPhalanximine> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveMutableSeed: true))
            return;

        plantHolderComp.Seed!.Viable = true;
    }

    private void OnExecutePlantRestoreSeeds(ref ExecuteEntityEffectEvent<PlantRestoreSeeds> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp, mustHaveMutableSeed: true))
            return;

        if (plantHolderComp.Seed!.Seedless)
        {
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            _popup.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.Args.TargetEntity);
            plantHolderComp.Seed.Seedless = false;
        }
    }

    private void OnExecuteRobustHarvest(ref ExecuteEntityEffectEvent<RobustHarvest> args)
    {
        if (!CanMetabolizePlant(args.Args.TargetEntity, out var plantHolderComp))
            return;

        if (plantHolderComp.Seed == null)
            return;

        if (plantHolderComp.Seed.Potency < args.Effect.PotencyLimit)
        {
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed.Potency = Math.Min(plantHolderComp.Seed.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit);

            if (plantHolderComp.Seed.Potency > args.Effect.PotencySeedlessThreshold)
            {
                plantHolderComp.Seed.Seedless = true;
            }
        }
        else if (plantHolderComp.Seed.Yield > 1 && _random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield
            _plantHolder.EnsureUniqueSeed(args.Args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed.Yield--;
        }
    }

    private void OnExecuteAdjustTemperature(ref ExecuteEntityEffectEvent<AdjustTemperature> args)
    {
        if (TryComp(args.Args.TargetEntity, out TemperatureComponent? temp))
        {
            var amount = args.Effect.Amount;

            if (args.Args is EntityEffectReagentArgs reagentArgs)
            {
                amount *= reagentArgs.Scale.Float();
            }

            _temperature.ChangeHeat(args.Args.TargetEntity, amount, true, temp);
        }
    }

    private void OnExecuteAreaReactionEffect(ref ExecuteEntityEffectEvent<AreaReactionEffect> args)
    {
        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Source == null)
                return;

            var spreadAmount = (int) Math.Max(0, Math.Ceiling((reagentArgs.Quantity / args.Effect.OverflowThreshold).Float()));
            var splitSolution = reagentArgs.Source.SplitSolution(reagentArgs.Source.Volume);
            var transform = EntityManager.GetComponent<TransformComponent>(reagentArgs.TargetEntity);
            var mapCoords = _xform.GetMapCoordinates(reagentArgs.TargetEntity, xform: transform);

            if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid) ||
                !_map.TryGetTileRef(gridUid, grid, transform.Coordinates, out var tileRef))
            {
                return;
            }

            if (_spreader.RequiresFloorToSpread(args.Effect.PrototypeId) && tileRef.Tile.IsSpace())
                return;

            var coords = _map.MapToGrid(gridUid, mapCoords);
            var ent = EntityManager.SpawnEntity(args.Effect.PrototypeId, coords.SnapToGrid());

            _smoke.StartSmoke(ent, splitSolution, args.Effect.Duration, spreadAmount);

            _audio.PlayPvs(args.Effect.Sound, reagentArgs.TargetEntity, AudioParams.Default.WithVariation(0.25f));
            return;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    private void OnExecuteCauseZombieInfection(ref ExecuteEntityEffectEvent<CauseZombieInfection> args)
    {
        EnsureComp<ZombifyOnDeathComponent>(args.Args.TargetEntity);
        EnsureComp<PendingZombieComponent>(args.Args.TargetEntity);
    }

    private void OnExecuteChemCleanBloodstream(ref ExecuteEntityEffectEvent<ChemCleanBloodstream> args)
    {
        var cleanseRate = args.Effect.CleanseRate;
        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Source == null || reagentArgs.Reagent == null)
                return;

            cleanseRate *= reagentArgs.Scale.Float();
            _bloodstream.FlushChemicals(args.Args.TargetEntity, reagentArgs.Reagent.ID, cleanseRate);
        }
        else
        {
            _bloodstream.FlushChemicals(args.Args.TargetEntity, "", cleanseRate);
        }
    }

    private void OnExecuteChemVomit(ref ExecuteEntityEffectEvent<ChemVomit> args)
    {
        if (args.Args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;

        _vomit.Vomit(args.Args.TargetEntity, args.Effect.ThirstAmount, args.Effect.HungerAmount);
    }

    private void OnExecuteCreateEntityReactionEffect(ref ExecuteEntityEffectEvent<CreateEntityReactionEffect> args)
    {
        var transform = Comp<TransformComponent>(args.Args.TargetEntity);
        var quantity = (int)args.Effect.Number;
        if (args.Args is EntityEffectReagentArgs reagentArgs)
            quantity *= reagentArgs.Quantity.Int();

        for (var i = 0; i < quantity; i++)
        {
            var uid = Spawn(args.Effect.Entity, _xform.GetMapCoordinates(args.Args.TargetEntity, xform: transform));
            _xform.AttachToGridOrMap(uid);

            // TODO figure out how to properly spawn inside of containers
            // e.g. cheese:
            // if the user is holding a bowl milk & enzyme, should drop to floor, not attached to the user.
            // if reaction happens in a backpack, should insert cheese into backpack.
            // --> if it doesn't fit, iterate through parent storage until it attaches to the grid (again, DON'T attach to players).
            // if the reaction happens INSIDE a stomach? the bloodstream? I have no idea how to handle that.
            // presumably having cheese materialize inside of your blood would have "disadvantages".
        }
    }

    private void OnExecuteCreateGas(ref ExecuteEntityEffectEvent<CreateGas> args)
    {
        var tileMix = _atmosphere.GetContainingMixture(args.Args.TargetEntity, false, true);

        if (tileMix != null)
        {
            if (args.Args is EntityEffectReagentArgs reagentArgs)
            {
                tileMix.AdjustMoles(args.Effect.Gas, reagentArgs.Quantity.Float() * args.Effect.Multiplier);
            }
            else
            {
                tileMix.AdjustMoles(args.Effect.Gas, args.Effect.Multiplier);
            }
        }
    }

    private void OnExecuteCureZombieInfection(ref ExecuteEntityEffectEvent<CureZombieInfection> args)
    {
        if (HasComp<IncurableZombieComponent>(args.Args.TargetEntity))
            return;

        RemComp<ZombifyOnDeathComponent>(args.Args.TargetEntity);
        RemComp<PendingZombieComponent>(args.Args.TargetEntity);

        if (args.Effect.Innoculate)
        {
            EnsureComp<ZombieImmuneComponent>(args.Args.TargetEntity);
        }
    }

    private void OnExecuteEmote(ref ExecuteEntityEffectEvent<Emote> args)
    {
        if (args.Effect.EmoteId == null)
            return;

        if (args.Effect.ShowInChat)
            _chat.TryEmoteWithChat(args.Args.TargetEntity, args.Effect.EmoteId, ChatTransmitRange.GhostRangeLimit, forceEmote: args.Effect.Force);
        else
            _chat.TryEmoteWithoutChat(args.Args.TargetEntity, args.Effect.EmoteId);
    }

    private void OnExecuteEmpReactionEffect(ref ExecuteEntityEffectEvent<EmpReactionEffect> args)
    {
        var transform = EntityManager.GetComponent<TransformComponent>(args.Args.TargetEntity);

        var range = args.Effect.EmpRangePerUnit;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            range = MathF.Min((float) (reagentArgs.Quantity * args.Effect.EmpRangePerUnit), args.Effect.EmpMaxRange);
        }

        _emp.EmpPulse(_xform.GetMapCoordinates(args.Args.TargetEntity, xform: transform),
            range,
            args.Effect.EnergyConsumption,
            args.Effect.DisableDuration);
    }

    private void OnExecuteExplosionReactionEffect(ref ExecuteEntityEffectEvent<ExplosionReactionEffect> args)
    {
        var intensity = args.Effect.IntensityPerUnit;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            intensity = MathF.Min((float) reagentArgs.Quantity * args.Effect.IntensityPerUnit, args.Effect.MaxTotalIntensity);
        }

        _explosion.QueueExplosion(
            args.Args.TargetEntity,
            args.Effect.ExplosionType,
            intensity,
            args.Effect.IntensitySlope,
            args.Effect.MaxIntensity,
            args.Effect.TileBreakScale);
    }

    private void OnExecuteFlammableReaction(ref ExecuteEntityEffectEvent<FlammableReaction> args)
    {
        if (!TryComp(args.Args.TargetEntity, out FlammableComponent? flammable))
            return;

        // Sets the multiplier for FireStacks to MultiplierOnExisting is 0 or greater and target already has FireStacks
        var multiplier = flammable.FireStacks != 0f && args.Effect.MultiplierOnExisting >= 0 ? args.Effect.MultiplierOnExisting : args.Effect.Multiplier;
        var quantity = 1f;
        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            quantity = reagentArgs.Quantity.Float();
            _flammable.AdjustFireStacks(args.Args.TargetEntity, quantity * multiplier, flammable);
            if (reagentArgs.Reagent != null)
                reagentArgs.Source?.RemoveReagent(reagentArgs.Reagent.ID, reagentArgs.Quantity);
        }
        else
        {
            _flammable.AdjustFireStacks(args.Args.TargetEntity, multiplier, flammable);
        }
    }

    private void OnExecuteFlashReactionEffect(ref ExecuteEntityEffectEvent<FlashReactionEffect> args)
    {
        var transform = EntityManager.GetComponent<TransformComponent>(args.Args.TargetEntity);

        var range = 1f;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
            range = MathF.Min((float)(reagentArgs.Quantity * args.Effect.RangePerUnit), args.Effect.MaxRange);

        _flash.FlashArea(
            args.Args.TargetEntity,
            null,
            range,
            args.Effect.Duration * 1000,
            slowTo: args.Effect.SlowTo,
            sound: args.Effect.Sound);

        if (args.Effect.FlashEffectPrototype == null)
            return;

        var uid = EntityManager.SpawnEntity(args.Effect.FlashEffectPrototype, _xform.GetMapCoordinates(transform));
        _xform.AttachToGridOrMap(uid);

        if (!TryComp<PointLightComponent>(uid, out var pointLightComp))
            return;

        _pointLight.SetRadius(uid, MathF.Max(1.1f, range), pointLightComp);
    }

    private void OnExecuteIgnite(ref ExecuteEntityEffectEvent<Ignite> args)
    {
        if (!TryComp(args.Args.TargetEntity, out FlammableComponent? flammable))
            return;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            _flammable.Ignite(reagentArgs.TargetEntity, reagentArgs.OrganEntity ?? reagentArgs.TargetEntity, flammable: flammable);
        }
        else
        {
            _flammable.Ignite(args.Args.TargetEntity, args.Args.TargetEntity, flammable: flammable);
        }
    }

    private void OnExecuteMakeSentient(ref ExecuteEntityEffectEvent<MakeSentient> args)
    {
        var uid = args.Args.TargetEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        RemComp<ReplacementAccentComponent>(uid);
        RemComp<MonkeyAccentComponent>(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (TryComp(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        ghostRole = AddComp<GhostRoleComponent>(uid);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);

        var entityData = EntityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }

    private void OnExecuteModifyBleedAmount(ref ExecuteEntityEffectEvent<ModifyBleedAmount> args)
    {
        if (TryComp<BloodstreamComponent>(args.Args.TargetEntity, out var blood))
        {
            var amt = args.Effect.Amount;
            if (args.Args is EntityEffectReagentArgs reagentArgs) {
                if (args.Effect.Scaled)
                    amt *= reagentArgs.Quantity.Float();
                amt *= reagentArgs.Scale.Float();
            }

            _bloodstream.TryModifyBleedAmount(args.Args.TargetEntity, amt, blood);
        }
    }

    private void OnExecuteModifyBloodLevel(ref ExecuteEntityEffectEvent<ModifyBloodLevel> args)
    {
        if (TryComp<BloodstreamComponent>(args.Args.TargetEntity, out var blood))
        {
            var amt = args.Effect.Amount;
            if (args.Args is EntityEffectReagentArgs reagentArgs)
            {
                if (args.Effect.Scaled)
                    amt *= reagentArgs.Quantity;
                amt *= reagentArgs.Scale;
            }

            _bloodstream.TryModifyBloodLevel(args.Args.TargetEntity, amt, blood);
        }
    }

    private void OnExecuteModifyLungGas(ref ExecuteEntityEffectEvent<ModifyLungGas> args)
    {
        LungComponent? lung;
        float amount = 1f;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            if (!TryComp<LungComponent>(reagentArgs.OrganEntity, out var organLung))
                return;
            lung = organLung;
            amount = reagentArgs.Quantity.Float();
        }
        else
        {
            if (!TryComp<LungComponent>(args.Args.TargetEntity, out var organLung)) //Likely needs to be modified to ensure it works correctly
                return;
            lung = organLung;
        }

        if (lung != null)
        {
            foreach (var (gas, ratio) in args.Effect.Ratios)
            {
                var quantity = ratio * amount / Atmospherics.BreathMolesToReagentMultiplier;
                if (quantity < 0)
                    quantity = Math.Max(quantity, -lung.Air[(int) gas]);
                lung.Air.AdjustMoles(gas, quantity);
            }
        }
    }

    private void OnExecuteOxygenate(ref ExecuteEntityEffectEvent<Oxygenate> args)
    {
        var multiplier = 1f;
        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            multiplier = reagentArgs.Quantity.Float();
        }

        if (TryComp<RespiratorComponent>(args.Args.TargetEntity, out var resp))
        {
            _respirator.UpdateSaturation(args.Args.TargetEntity, multiplier * args.Effect.Factor, resp);
        }
    }

    private void OnExecutePlantMutateChemicals(ref ExecuteEntityEffectEvent<PlantMutateChemicals> args)
    {
        var plantholder = EntityManager.GetComponent<PlantHolderComponent>(args.Args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var chemicals = plantholder.Seed.Chemicals;
        var randomChems = _protoManager.Index<WeightedRandomFillSolutionPrototype>("RandomPickBotanyReagent").Fills;

        // Add a random amount of a random chemical to this set of chemicals
        if (randomChems != null)
        {
            var pick = _random.Pick<RandomFillSolution>(randomChems);
            var chemicalId = _random.Pick(pick.Reagents);
            var amount = _random.Next(1, (int)pick.Quantity);
            var seedChemQuantity = new SeedChemQuantity();
            if (chemicals.ContainsKey(chemicalId))
            {
                seedChemQuantity.Min = chemicals[chemicalId].Min;
                seedChemQuantity.Max = chemicals[chemicalId].Max + amount;
            }
            else
            {
                seedChemQuantity.Min = 1;
                seedChemQuantity.Max = 1 + amount;
                seedChemQuantity.Inherent = false;
            }
            var potencyDivisor = (int)Math.Ceiling(100.0f / seedChemQuantity.Max);
            seedChemQuantity.PotencyDivisor = potencyDivisor;
            chemicals[chemicalId] = seedChemQuantity;
        }
    }

    private void OnExecutePlantMutateConsumeGasses(ref ExecuteEntityEffectEvent<PlantMutateConsumeGasses> args)
    {
        var plantholder = EntityManager.GetComponent<PlantHolderComponent>(args.Args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var gasses = plantholder.Seed.ExudeGasses;

        // Add a random amount of a random gas to this gas dictionary
        float amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        Gas gas = _random.Pick(Enum.GetValues(typeof(Gas)).Cast<Gas>().ToList());
        if (gasses.ContainsKey(gas))
        {
            gasses[gas] += amount;
        }
        else
        {
            gasses.Add(gas, amount);
        }
    }

    private void OnExecutePlantMutateExudeGasses(ref ExecuteEntityEffectEvent<PlantMutateExudeGasses> args)
    {
        var plantholder = EntityManager.GetComponent<PlantHolderComponent>(args.Args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        var gasses = plantholder.Seed.ConsumeGasses;

        // Add a random amount of a random gas to this gas dictionary
        float amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        Gas gas = _random.Pick(Enum.GetValues(typeof(Gas)).Cast<Gas>().ToList());
        if (gasses.ContainsKey(gas))
        {
            gasses[gas] += amount;
        }
        else
        {
            gasses.Add(gas, amount);
        }
    }

    private void OnExecutePlantMutateHarvest(ref ExecuteEntityEffectEvent<PlantMutateHarvest> args)
    {
        var plantholder = EntityManager.GetComponent<PlantHolderComponent>(args.Args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        if (plantholder.Seed.HarvestRepeat == HarvestType.NoRepeat)
            plantholder.Seed.HarvestRepeat = HarvestType.Repeat;
        else if (plantholder.Seed.HarvestRepeat == HarvestType.Repeat)
            plantholder.Seed.HarvestRepeat = HarvestType.SelfHarvest;
    }

    private void OnExecutePlantSpeciesChange(ref ExecuteEntityEffectEvent<PlantSpeciesChange> args)
    {
        var plantholder = EntityManager.GetComponent<PlantHolderComponent>(args.Args.TargetEntity);
        if (plantholder.Seed == null)
            return;

        if (plantholder.Seed.MutationPrototypes.Count == 0)
            return;

        var targetProto = _random.Pick(plantholder.Seed.MutationPrototypes);
        _protoManager.TryIndex(targetProto, out SeedPrototype? protoSeed);

        if (protoSeed == null)
        {
            Log.Error($"Seed prototype could not be found: {targetProto}!");
            return;
        }

        plantholder.Seed = plantholder.Seed.SpeciesChange(protoSeed);
    }

    private void OnExecutePolymorph(ref ExecuteEntityEffectEvent<PolymorphEffect> args)
    {
        // Make it into a prototype
        EnsureComp<PolymorphableComponent>(args.Args.TargetEntity);
        _polymorph.PolymorphEntity(args.Args.TargetEntity, args.Effect.PolymorphPrototype);
    }

    private void OnExecuteResetNarcolepsy(ref ExecuteEntityEffectEvent<ResetNarcolepsy> args)
    {
        if (args.Args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;

        _narcolepsy.AdjustNarcolepsyTimer(args.Args.TargetEntity, args.Effect.TimerReset);
    }
}

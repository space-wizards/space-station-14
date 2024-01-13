using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Rotting;
using Content.Server.Beam;
using Content.Server.Bed.Sleep;
using Content.Server.Bible.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.StatusEffect;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Frozen;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly BeamSystem _beam = default!;

    private FrozenDictionary<string, VampireAbilityListPrototype> _cachedAbilityLists = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string SleepStatusEffectKey = "ForcedSleep";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string HolyWaterKey = "Holywater";
    [ValidatePrototypeId<PolymorphPrototype>]
    private const string VampireBatKey = "VampireBat";
    [ValidatePrototypeId<EmotePrototype>]
    private const string ScreamEmoteKey = "Scream";

    private ReagentPrototype _holyWater = default!;
    private PolymorphPrototype _vampireBat = default!;
    private EmotePrototype _scream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractHandEvent>(OnInteractWithHumanoid, before: new[] { typeof(InteractionPopupSystem), typeof(SleepingSystem) });
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodEvent>(DrinkDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseEvent>(HypnotiseDoAfter);

        SubscribeLocalEvent<VampireComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<VampireComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<VampireComponent, MobStateChangedEvent>(OnVampireStateChanged);
        SubscribeLocalEvent<VampireComponent, VampireUseAreaPowerEvent>(OnUseAreaPower);
        SubscribeLocalEvent<VampireComponent, VampireUseTargetedPowerEvent>(OnUseTargetedPower);
        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(OnExamined);

        CachePowers();

        //Local references
        _holyWater = _prototypeManager.Index<ReagentPrototype>(HolyWaterKey);
        _vampireBat = _prototypeManager.Index<PolymorphPrototype>(VampireBatKey);
        _scream = _prototypeManager.Index<EmotePrototype>(ScreamEmoteKey);
    }

    /// <summary>
    /// Convert the entity into a vampire
    /// </summary>
    private void OnComponentInit(EntityUid uid, VampireComponent component, ComponentInit args)
    {
        //_solution.EnsureSolution(uid, component.BloodContainer);
        RemComp<BarotraumaComponent>(uid);
        RemComp<PerishableComponent>(uid);
        EnsureComp<UnholyComponent>(uid);

        if (TryComp<TemperatureComponent>(uid, out var temperatureComponent))
            temperatureComponent.ColdDamageThreshold = 0;

        //Hardcoding the default ability list
        //TODO: Add client UI and multiple ability lists
        component.ChosenAbilityList = _cachedAbilityLists["Default"];

        ConvertBody(uid, component.ChosenAbilityList);

        UpdateAbilities((uid, component), true);
    }

    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (component.ActiveAbilities.Contains(VampirePowerKey.ToggleFangs) && args.IsInDetailsRange)
            args.AddMarkup($"{Loc.GetString("vampire-fangs-extended-examine")}{Environment.NewLine}");
    }

    /// <summary>
    /// Upon using any non targeted power
    /// </summary>
    private void OnUseAreaPower(EntityUid uid, VampireComponent component, VampireUseAreaPowerEvent args)
    {
        Entity<VampireComponent> vampire = (uid, component);

        TriggerPower(vampire, args.Type, null);
    }
    private void OnUseTargetedPower(EntityUid uid, VampireComponent component, VampireUseTargetedPowerEvent args)
    {
        Entity<VampireComponent> vampire = (uid, component);

        TriggerPower(vampire, args.Type, args.Target);
    }

    private void TriggerPower(Entity<VampireComponent> vampire, VampirePowerKey powerType, EntityUid? target)
    {
        if (!vampire.Comp.UnlockedPowers.ContainsKey(powerType))
            return;

        if (!GetAbilityDefinition(vampire.Comp, powerType, out var def) || def == null)
            return;

        if (def.ActivationCost > 0 && def.ActivationCost > vampire.Comp.AvailableBlood)
        {
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), vampire, vampire, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        //Block if we are cuffed
        if (!def.UsableWhileCuffed && TryComp<CuffableComponent>(vampire, out var cuffable) && !cuffable.CanStillInteract)
        {
            _popup.PopupEntity(Loc.GetString("vampire-cuffed"), vampire, vampire, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        //Block if we are stunned
        if (!def.UsableWhileStunned && HasComp<StunnedComponent>(vampire))
        {
            _popup.PopupEntity(Loc.GetString("vampire-stunned"), vampire, vampire, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        //Block if we are muzzled - so far only one item does this?
        if (!def.UsableWhileMuffled && TryComp<ReplacementAccentComponent>(vampire, out var accent) && accent.Accent.Equals("mumble"))
        {
            _popup.PopupEntity(Loc.GetString("vampire-muffled"), vampire, vampire, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        if (def.ActivationEffect != null)
            Spawn(def.ActivationEffect, _transform.GetMapCoordinates(Transform(vampire.Owner)));

        var success = true;

        //TODO: Rewrite when a magic effect system is introduced (like reagents)
        switch (powerType)
        {
            case VampirePowerKey.ToggleFangs:
                {
                    ToggleFangs(vampire);
                    break;
                }
            case VampirePowerKey.DeathsEmbrace:
                {
                    success = TryMoveToCoffin(vampire);
                    break;
                }
            case VampirePowerKey.Glare:
                {
                    Glare(vampire, target, def.Duration, def.Damage);
                    break;
                }
            case VampirePowerKey.Screech:
                {
                    Screech(vampire, def.Duration, def.Damage);
                    break;
                }
            case VampirePowerKey.BatForm:
                {
                    PolymorphBat(vampire);
                    break;
                }
            case VampirePowerKey.Hypnotise:
                {
                    success = TryHypnotise(vampire, target, def.Duration, def.Delay);
                    break;
                }
            case VampirePowerKey.BloodSteal:
                {
                    BloodSteal(vampire);
                    break;
                }
            case VampirePowerKey.CloakOfDarkness:
                {
                    CloakOfDarkness(vampire);
                    break;
                }
            default:
                break;
        }

        if (!success)
            return;

        AddBlood(vampire, -def.ActivationCost);

        _action.StartUseDelay(vampire.Comp.UnlockedPowers[powerType]);
    }

    /// <summary>
    /// Handles healing and damaging in space
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var vampireComponent))
        {
            var vampire = (uid, vampireComponent);

            if (TryComp<VampireSealthComponent>(uid, out var vampireSealthComponent))
            {
                if (vampireSealthComponent.NextStealthTick <= 0)
                {
                    vampireSealthComponent.NextStealthTick = 1;
                    AddBlood(vampire, vampireComponent.ChosenAbilityList.StealthBloodCost);
                }
                vampireSealthComponent.NextStealthTick -= frameTime;
            }

            if (TryComp<VampireHealingComponent>(uid, out var vampireHealingComponent))
            {
                if (vampireHealingComponent.NextHealTick <= 0)
                {
                    vampireHealingComponent.NextHealTick = 1;
                    DoCoffinHeal(vampire);
                }
                vampireHealingComponent.NextHealTick -= frameTime;
            }

            if (IsInSpace(uid))
            {
                if (vampireComponent.NextSpaceDamageTick <= 0)
                {
                    vampireComponent.NextSpaceDamageTick = 1;
                    DoSpaceDamage(vampire);
                }
                vampireComponent.NextSpaceDamageTick -= frameTime;
            }
        }
    }

    /// <summary>
    /// Update which abilities are available based upon available blood
    /// </summary>
    private void UpdateAbilities(Entity<VampireComponent> vampire, bool silent = false)
    {
        foreach (var power in vampire.Comp.ChosenAbilityList.Abilities)
        {
            if (power.BloodUnlockRequirement <= vampire.Comp.AvailableBlood)
                UnlockAbility(vampire, power, silent);
        }
    }
    private void UnlockAbility(Entity<VampireComponent> vampire, VampireAbilityEntry powerDef, bool silent = false)
    {
        if (vampire.Comp.UnlockedPowers.ContainsKey(powerDef.Type))
            return;

        if (powerDef.ActionPrototype == null)
        {
            //passive ability
            vampire.Comp.UnlockedPowers.Add(powerDef.Type, null);
        }
        else
        {
            var actionUid = _action.AddAction(vampire.Owner, powerDef.ActionPrototype);
            if (!actionUid.HasValue)
                return;
            vampire.Comp.UnlockedPowers.Add(powerDef.Type, actionUid.Value);
        }

        if (!silent)
            RaiseNetworkEvent(new VampireAbilityUnlockedEvent() { UnlockedAbility = powerDef.Type }, vampire);
    }

    #region Other Powers
    private void Screech(Entity<VampireComponent> vampire, float duration, DamageSpecifier? damage = null)
    {
        var transform = Transform(vampire.Owner);

        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, 3, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            if (HasComp<BibleUserComponent>(entity))
                continue;

            if (HasComp<VampireComponent>(entity))
                continue;

            if (HasComp<HumanoidAppearanceComponent>(entity))
            {
                _stun.TryParalyze(entity, TimeSpan.FromSeconds(duration), false);
                _chat.TryEmoteWithoutChat(entity, _scream, true);
            }

            if (damage != null)
                _damageableSystem.TryChangeDamage(entity, damage);
        }
    }
    private void Glare(Entity<VampireComponent> vampire, EntityUid? target, float duration, DamageSpecifier? damage = null)
    {
        if (!target.HasValue)
            return;

        if (HasComp<VampireComponent>(target))
            return;

        if (!HasComp<FlashableComponent>(target))
            return;

        if (HasComp<FlashImmunityComponent>(target))
            return;

        if (HasComp<BibleUserComponent>(target))
        {
            _stun.TryParalyze(vampire.Owner, TimeSpan.FromSeconds(duration), true);
            _chat.TryEmoteWithoutChat(vampire.Owner, _scream, true);
            if (damage != null)
                _damageableSystem.TryChangeDamage(vampire.Owner, damage);

            return;
        }

        _stun.TryParalyze(target.Value, TimeSpan.FromSeconds(duration), true);
    }
    private void PolymorphBat(Entity<VampireComponent> vampire)
    {
        _polymorph.PolymorphEntity(vampire, _vampireBat);
    }
    private void BloodSteal(Entity<VampireComponent> vampire)
    {
        var transform = Transform(vampire.Owner);

        var targets = new HashSet<EntityUid>();

        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, 3, LookupFlags.Approximate | LookupFlags.Dynamic))
        {
            if (entity == vampire.Owner)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(entity))
                continue;

            if (_rotting.IsRotten(entity))
                continue;

            if (HasComp<BibleUserComponent>(entity))
                continue;

            if (!TryComp<BloodstreamComponent>(entity, out var bloodstream) || bloodstream.BloodSolution == null)
                continue;

            var victimBloodRemaining = bloodstream.BloodSolution.Value.Comp.Solution.Volume;
            if (victimBloodRemaining <= 0)
                continue;

            var volumeToConsume = (FixedPoint2) Math.Min((float) victimBloodRemaining.Value, 20); //HARDCODE, 20u of blood per person per use

            targets.Add(entity);

            //Transfer 80% to the vampire
            var bloodSolution = _solution.SplitSolution(bloodstream.BloodSolution.Value, volumeToConsume * 0.80);
            //And spill 20% on the floor
            _blood.TryModifyBloodLevel(entity, -(volumeToConsume * 0.2));

            //Dont check this time, if we are full - just continue anyway
            TryIngestBlood(vampire, bloodSolution);

            AddBlood(vampire, volumeToConsume * 0.80);

            _beam.TryCreateBeam(vampire, entity, "Lightning");

            _popup.PopupEntity(Loc.GetString("vampire-bloodsteal-other"), entity, entity, Shared.Popups.PopupType.LargeCaution);
        }



        //Update abilities, add new unlocks
        UpdateAbilities(vampire);
    }
    private void CloakOfDarkness(Entity<VampireComponent> vampire)
    {
        if (vampire.Comp.ActiveAbilities.Contains(VampirePowerKey.CloakOfDarkness))
        {
            vampire.Comp.ActiveAbilities.Remove(VampirePowerKey.CloakOfDarkness);
            _action.SetToggled(vampire.Comp.UnlockedPowers[VampirePowerKey.CloakOfDarkness], false);
            RemComp<StealthOnMoveComponent>(vampire);
            RemComp<StealthComponent>(vampire);
            RemComp<VampireSealthComponent>(vampire);
            _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), vampire, vampire);
        }
        else
        {
            vampire.Comp.ActiveAbilities.Add(VampirePowerKey.CloakOfDarkness);
            _action.SetToggled(vampire.Comp.UnlockedPowers[VampirePowerKey.CloakOfDarkness], true);
            EnsureComp<StealthComponent>(vampire);
            EnsureComp<StealthOnMoveComponent>(vampire);
            EnsureComp<VampireSealthComponent>(vampire);
            _popup.PopupEntity(Loc.GetString("vampire-cloak-enable"), vampire, vampire);
        }
    }
    #endregion

    #region Hypnotise
    private bool TryHypnotise(Entity<VampireComponent> vampire, EntityUid? target, float duration, float delay)
    {
        if (target == null)
            return false;

        var attempt = new FlashAttemptEvent(target.Value, vampire.Owner, vampire.Owner);
        RaiseLocalEvent(target.Value, attempt, true);

        if (attempt.Cancelled)
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, TimeSpan.FromSeconds(delay),
        new VampireHypnotiseEvent(duration),
        eventTarget: vampire,
        target: target,
        used: target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterEventArgs))
        {
            _popup.PopupEntity(Loc.GetString("vampire-hypnotise-other"), target.Value, Shared.Popups.PopupType.SmallCaution);
        }
        else
        {
            return false;
        }
        return true;
    }
    private void HypnotiseDoAfter(Entity<VampireComponent> vampire, ref VampireHypnotiseEvent args)
    {
        if (!args.Target.HasValue)
            return;

        if (args.Cancelled)
            return;
        //Do checks
        //Force sleep 30seconds
        _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(args.Target.Value, SleepStatusEffectKey, TimeSpan.FromSeconds(args.Duration), false);
    }
    #endregion

    #region Deaths Embrace
    private void OnVampireStateChanged(EntityUid uid, VampireComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            OnUseAreaPower(uid, component, new() { Type = VampirePowerKey.DeathsEmbrace });
    }
    private void OnInsertedIntoContainer(EntityUid uid, VampireComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<CoffinComponent>(args.Container.Owner, out var coffinComp))
        {
            component.HomeCoffin = args.Container.Owner;
            EnsureComp<VampireHealingComponent>(args.Entity);
            _popup.PopupEntity(Loc.GetString("vampire-deathsembrace-bind"), uid, uid);
        }
    }
    private void OnRemovedFromContainer(EntityUid uid, VampireComponent component, EntGotRemovedFromContainerMessage args)
    {
        RemCompDeferred<VampireHealingComponent>(args.Entity);
    }
    private bool TryMoveToCoffin(Entity<VampireComponent> vampire)
    {
        if (!vampire.Comp.HomeCoffin.HasValue)
            return false;

        if (!TryComp<EntityStorageComponent>(vampire.Comp.HomeCoffin, out var coffinEntityStorage))
            return false;

        if (!_entityStorage.CanInsert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage))
            return false;

        Spawn("Smoke", Transform(vampire).Coordinates);

        _entityStorage.CloseStorage(vampire.Comp.HomeCoffin.Value, coffinEntityStorage);

        return _entityStorage.Insert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage);
    }
    private void DoCoffinHeal(Entity<VampireComponent> vampire)
    {
        if (!_container.TryGetOuterContainer(vampire.Owner, Transform(vampire.Owner), out var container))
            return;

        if (!HasComp<CoffinComponent>(container.Owner))
            return;

        //Heal the vampire
        if (!GetAbilityDefinition(vampire.Comp, VampirePowerKey.DeathsEmbrace, out var healing) || healing == null)
            return;

        _damageableSystem.TryChangeDamage(vampire.Owner, healing.Damage, true, origin: container.Owner);

        //If they are dead and we are below the death threshold - revive
        if (!TryComp<MobStateComponent>(vampire, out var mobStateComponent))
            return;

        if (!_mobState.IsDead(vampire, mobStateComponent))
            return;

        if (!_mobThreshold.TryGetThresholdForState(vampire, MobState.Dead, out var threshold))
            return;

        if (!TryComp<DamageableComponent>(vampire, out var damageableComponent))
            return;

        //Should be around 150 total damage ish
        if (damageableComponent.TotalDamage < threshold * 0.75)
        {
            _mobState.ChangeMobState(vampire, MobState.Critical, mobStateComponent, container.Owner);
        }
    }
    #endregion

    #region Blood Drinking
    private void ToggleFangs(Entity<VampireComponent> vampire)
    {
        var popupText = string.Empty;
        if (vampire.Comp.ActiveAbilities.Contains(VampirePowerKey.ToggleFangs))
        {
            vampire.Comp.ActiveAbilities.Remove(VampirePowerKey.ToggleFangs);
            _action.SetToggled(vampire.Comp.UnlockedPowers[VampirePowerKey.ToggleFangs], false);
            popupText = Loc.GetString("vampire-fangs-retracted");
        }
        else
        {
            vampire.Comp.ActiveAbilities.Add(VampirePowerKey.ToggleFangs);
            _action.SetToggled(vampire.Comp.UnlockedPowers[VampirePowerKey.ToggleFangs], true);
            popupText = Loc.GetString("vampire-fangs-extended");
        }
        _popup.PopupEntity(popupText, vampire.Owner, vampire.Owner);
    }
    private void OnInteractWithHumanoid(EntityUid uid, HumanoidAppearanceComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;

        if (!TryComp<VampireComponent>(args.User, out var vampireComponent))
            return;

        args.Handled = TryDrink((args.User, vampireComponent), args.Target);
    }
    private bool TryDrink(Entity<VampireComponent> vampire, EntityUid target)
    {

        //Do a precheck
        if (!vampire.Comp.ActiveAbilities.Contains(VampirePowerKey.ToggleFangs))
            return false;

        if (!_interaction.InRangeUnobstructed(vampire, target, popup: true))
            return false;

        if (_food.IsMouthBlocked(target, vampire))
            return false;

        if (_rotting.IsRotten(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), vampire, vampire, Shared.Popups.PopupType.SmallCaution);
            return false;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, vampire.Comp.ChosenAbilityList.BloodDrainFrequency,
        new VampireDrinkBloodEvent(),
        eventTarget: vampire,
        target: target,
        used: target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
    private void DrinkDoAfter(Entity<VampireComponent> entity, ref VampireDrinkBloodEvent args)
    {
        if (!args.Target.HasValue)
            return;

        if (args.Cancelled)
            return;

        if (_food.IsMouthBlocked(args.Target.Value, entity))
            return;

        if (!entity.Comp.ActiveAbilities.Contains(VampirePowerKey.ToggleFangs))
            return;

        if (_rotting.IsRotten(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), args.User, args.User, Shared.Popups.PopupType.SmallCaution);
            return;
        }

        if (!TryComp<BloodstreamComponent>(args.Target, out var targetBloodstream) || targetBloodstream == null || targetBloodstream.BloodSolution == null)
            return;

        //Ensure there is enough blood to drain
        var victimBloodRemaining = targetBloodstream.BloodSolution.Value.Comp.Solution.Volume;
        if (victimBloodRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), entity.Owner, entity.Owner, Shared.Popups.PopupType.SmallCaution);
            return;
        }
        var volumeToConsume = (FixedPoint2) Math.Min((float) victimBloodRemaining.Value, entity.Comp.ChosenAbilityList.BloodDrainVolume);

        args.Repeat = true;

        //Transfer 95% to the vampire
        var bloodSolution = _solution.SplitSolution(targetBloodstream.BloodSolution.Value, volumeToConsume * 0.95);

        //Thou shall not feed upon the blood of the holy
        if (HasComp<BibleUserComponent>(args.Target))
        {
            bloodSolution.AddReagent(_holyWater.ID, 5);
            _popup.PopupEntity(Loc.GetString("vampire-ingest-holyblood"), entity, entity, Shared.Popups.PopupType.LargeCaution);
            args.Repeat = false;
        }

        if (!TryIngestBlood(entity, bloodSolution))
        {
            //Undo, put the blood back
            _solution.AddSolution(targetBloodstream.BloodSolution.Value, bloodSolution);
            args.Repeat = false;
            return;
        }

        //Slurp
        _audio.PlayPvs(entity.Comp.BloodDrainSound, entity.Owner, AudioParams.Default.WithVolume(-3f));

        AddBlood(entity, volumeToConsume * 0.95);

        //Update abilities, add new unlocks
        UpdateAbilities(entity);

        //And spill 5% on the floor
        _blood.TryModifyBloodLevel(args.Target.Value, -(volumeToConsume * 0.05));
    }
    private bool TryIngestBlood(Entity<VampireComponent> vampire, Solution ingestedSolution, bool force = false)
    {
        //Get all stomaches
        if (TryComp<BodyComponent>(vampire.Owner, out var body) && _body.TryGetBodyOrganComponents<StomachComponent>(vampire.Owner, out var stomachs, body))
        {
            //Pick the first one
            var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, ingestedSolution, stomach.Comp));
            if (firstStomach == null)
            {
                //We are full
                _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), vampire.Owner, vampire.Owner, Shared.Popups.PopupType.SmallCaution);
                return false;
            }
            //Fill the stomach with that delicious blood
            return _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, ingestedSolution, firstStomach.Value.Comp);
        }

        //No stomach
        return false;
    }
    private void AddBlood(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        vampire.Comp.TotalBloodDrank += quantity.Float();
        vampire.Comp.AvailableBlood += quantity.Float();
    }
    #endregion

    private bool GetAbilityDefinition(VampireComponent component, VampirePowerKey key, out VampireAbilityEntry? vampireAbilityEntry)
    {
        if (component.ChosenAbilityList == null)
        {
            vampireAbilityEntry = null;
            return false;
        }

        if (component.ChosenAbilityList.AbilitiesByKey.TryGetValue(key, out var entry))
        {
            vampireAbilityEntry = entry;
            return true;
        }

        vampireAbilityEntry = null;
        return false;
    }
    private void DoSpaceDamage(Entity<VampireComponent> vampire)
    {
        if (!GetAbilityDefinition(vampire.Comp, VampirePowerKey.StellarWeakness, out var def) || def == null)
            return;

        _damageableSystem.TryChangeDamage(vampire, def.Damage, true, origin: vampire);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), vampire, vampire, Shared.Popups.PopupType.LargeCaution);
    }
    private bool IsInSpace(EntityUid vampireUid)
    {
        var vampireTransform = Transform(vampireUid);
        var vampirePosition = _transform.GetMapCoordinates(vampireTransform);

        if (!_mapMan.TryFindGridAt(vampirePosition, out _, out var grid))
            return true;

        if (!_mapSystem.TryGetTileRef(vampireUid, grid, vampireTransform.Coordinates, out var tileRef))
            return true;

        return tileRef.Tile.IsEmpty;
    }


    private void CachePowers()
    {
        var tempDict = new Dictionary<string, VampireAbilityListPrototype>();

        var abilityLists = _prototypeManager.EnumeratePrototypes<VampireAbilityListPrototype>();
        var listAbilities = new Dictionary<VampirePowerKey, VampireAbilityEntry>();
        foreach (var abilityList in abilityLists)
        {
            tempDict.Add(abilityList.ID, abilityList);
            foreach (var ability in abilityList.Abilities)
            {
                listAbilities.TryAdd(ability.Type, ability);
            }
            abilityList.AbilitiesByKey = listAbilities.ToFrozenDictionary();
        }

        _cachedAbilityLists = tempDict.ToFrozenDictionary();
    }
}

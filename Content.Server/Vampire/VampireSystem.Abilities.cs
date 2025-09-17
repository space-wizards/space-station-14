using Content.Server.Bible.Components;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Polymorph;
using Content.Shared.Prying.Components;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Bed.Sleep;
using Content.Shared.Flash.Components;
using Content.Shared.Storage.Components;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    private FrozenDictionary<string, VampirePowerProtype> _powerCache = default!;
    private FrozenDictionary<string, VampirePassiveProtype> _passiveCache = default!;

    private void InitializePowers()
    {
        _powerCache = BuildPowerCache();
        _passiveCache = BuildPassiveCache();

        //Abilities
        SubscribeLocalEvent<VampireComponent, VampireOpenMutationsMenu>(OnVampireOpenMutationsMenu);
        SubscribeLocalEvent<VampireComponent, VampireToggleFangsEvent>(OnVampireToggleFangs);
        SubscribeLocalEvent<VampireComponent, VampireGlareEvent>(OnVampireGlare);
        SubscribeLocalEvent<VampireComponent, VampireScreechEvent>(OnVampireScreech);
        SubscribeLocalEvent<VampireComponent, VampirePolymorphEvent>(OnVampirePolymorph);
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseEvent>(OnVampireHypnotise);
        SubscribeLocalEvent<VampireComponent, VampireBloodStealEvent>(OnVampireBloodSteal);
        SubscribeLocalEvent<VampireComponent, VampireUnholyStrengthEvent>(OnVampireUnholyStrength);
        SubscribeLocalEvent<VampireComponent, VampireSupernaturalStrengthEvent>(OnVampireSupernaturalStrength);
        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessEvent>(OnVampireCloakOfDarkness);

        //Hypnotise
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseDoAfterEvent>(HypnotiseDoAfter);

        //Drink Blood
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(DrinkDoAfter);

        //Deaths embrace
        SubscribeLocalEvent<VampireDeathsEmbraceComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<VampireDeathsEmbraceComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<VampireDeathsEmbraceComponent, MobStateChangedEvent>(OnVampireStateChanged);

    }

    #region Ability Entry Points
    private void OnVampireOpenMutationsMenu(EntityUid uid, VampireComponent component, VampireOpenMutationsMenu ev)
    {
        TryOpenUi(uid, ev.Performer, component);
        ev.Handled = true;
    }
    private void OnVampireToggleFangs(EntityUid entity, VampireComponent component, VampireToggleFangsEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        var actionEntity = GetPowerEntity(vampire, def.ID);
        if (actionEntity == null)
            return;

        var toggled = ToggleFangs(vampire);

        _action.SetToggled(actionEntity, toggled);

        ev.Handled = true;
    }
    private void OnVampireGlare(EntityUid entity, VampireComponent component, VampireGlareEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        Glare(vampire, ev.Target, def.Duration, def.Damage);

        ev.Handled = true;
    }
    private void OnVampireScreech(EntityUid entity, VampireComponent component, VampireScreechEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        Screech(vampire, def.Duration, def.Damage);

        ev.Handled = true;
    }
    private void OnVampirePolymorph(EntityUid entity, VampireComponent component, VampirePolymorphEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        PolymorphSelf(vampire, def.PolymorphTarget);

        ev.Handled = true;
    }
    private void OnVampireHypnotise(EntityUid entity, VampireComponent component, VampireHypnotiseEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        ev.Handled = TryHypnotise(vampire, ev.Target, def.Duration, def.DoAfterDelay);
    }
    private void OnVampireBloodSteal(EntityUid entity, VampireComponent component, VampireBloodStealEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        BloodSteal(vampire);

        ev.Handled = true;
    }
    private void OnVampireUnholyStrength(EntityUid entity, VampireComponent component, VampireUnholyStrengthEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        UnnaturalStrength(vampire);

        ev.Handled = true;
    }
    private void OnVampireSupernaturalStrength(EntityUid entity, VampireComponent component, VampireSupernaturalStrengthEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (!IsAbilityUsable(vampire, def))
            return;

        SupernaturalStrength(vampire);

        ev.Handled = true;
    }
    private void OnVampireCloakOfDarkness(EntityUid entity, VampireComponent component, VampireCloakOfDarknessEvent ev)
    {
        if (!TryGetPowerDefinition(ev.DefinitionName, out var def))
            return;

        var vampire = new Entity<VampireComponent>(entity, component);

        if (_vampire.GetBloodEssence(vampire) < FixedPoint2.New(330))
        {
            _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), vampire, vampire);
            return;
        }

        if (!IsAbilityUsable(vampire, def))
            return;

        var actionEntity = GetPowerEntity(vampire.Comp, def.ID);
        if (actionEntity == null)
            return;

        var toggled = CloakOfDarkness(vampire, def.Upkeep, 0.75f, -0.5f);

        _action.SetToggled(actionEntity, toggled);

        ev.Handled = true;
    }
    private void OnInteractHandEvent(EntityUid uid, VampireComponent component, BeforeInteractHandEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        if (args.Target == uid)
            return;

        if (!TryGetPowerDefinition(VampireComponent.DrinkBloodPrototype, out var def))
            return;

        var vampire = new Entity<VampireComponent>(uid, component);

        args.Handled = TryDrink(vampire, args.Target, def.DoAfterDelay!.Value);
    }
    #endregion


    private bool TryGetPowerDefinition(string name, [NotNullWhen(true)] out VampirePowerProtype? definition)
        => _powerCache.TryGetValue(name, out definition);

    private bool IsAbilityUsable(Entity<VampireComponent> vampire, VampirePowerProtype def)
    {
        if (!IsPowerUnlocked(vampire, def.ID))
            return false;

        //Block if we are cuffed
        if (!def.UsableWhileCuffed && TryComp<CuffableComponent>(vampire, out var cuffable) && !cuffable.CanStillInteract)
        {
            _popup.PopupEntity(Loc.GetString("vampire-cuffed"), vampire, vampire, PopupType.MediumCaution);
            return false;
        }

        //Block if we are stunned
        if (!def.UsableWhileStunned && HasComp<StunnedComponent>(vampire))
        {
            _popup.PopupEntity(Loc.GetString("vampire-stunned"), vampire, vampire, PopupType.MediumCaution);
            return false;
        }

        //Block if we are muzzled - so far only one item does this?
        if (!def.UsableWhileMuffled && TryComp<ReplacementAccentComponent>(vampire, out var accent) && accent.Accent.Equals("mumble"))
        {
            _popup.PopupEntity(Loc.GetString("vampire-muffled"), vampire, vampire, PopupType.MediumCaution);
            return false;
        }

        //Block if we dont have enough essence
        if (def.ActivationCost > 0 && !SubtractBloodEssence(vampire, def.ActivationCost))
        {
            _popup.PopupClient(Loc.GetString("vampire-not-enough-blood"), vampire, vampire, PopupType.MediumCaution);
            return false;
        }

        //Check if we are near an anchored prayable entity - ie the chapel
        if (IsNearPrayable(vampire))
        {
            //Warning about holy power
            return false;
        }

        return true;
    }


    #region Activeable Gargantua Powers
    private void UnnaturalStrength(Entity<VampireComponent> vampire)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Slash", 10);
        var meleeComp = EnsureComp<MeleeWeaponComponent>(vampire);
        if (TryComp<VampireStrengthComponent>(vampire, out var strengthComp) && strengthComp.Power == "UnholyStrength")
        {
            meleeComp.Damage = meleeComp.Damage - damage;
            RemComp<VampireStrengthComponent>(vampire);
        }
        else if (!HasComp<VampireStrengthComponent>(vampire))
        {
            var strength = EnsureComp<VampireStrengthComponent>(vampire);
            strength.Upkeep = 1f;
            strength.Power = "UnholyStrength";
            _popup.PopupEntity(Loc.GetString("vampire-unnaturalstrength", ("user", vampire)), vampire, vampire, Shared.Popups.PopupType.SmallCaution);
            meleeComp.Damage += damage;
        }
    }
    private void SupernaturalStrength(Entity<VampireComponent> vampire)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Slash", 15);
        var meleeComp = EnsureComp<MeleeWeaponComponent>(vampire);
        if (TryComp<VampireStrengthComponent>(vampire, out var strengthComp) && strengthComp.Power == "SupernaturalStrength")
        {
            meleeComp.Damage = meleeComp.Damage - damage;
            RemComp<PryingComponent>(vampire);
            RemComp<VampireStrengthComponent>(vampire);
        }
        else if (!HasComp<VampireStrengthComponent>(vampire))
        {
            var strength = EnsureComp<VampireStrengthComponent>(vampire);
            strength.Upkeep = 1f;
            strength.Power = "SupernaturalStrength";

            var pryComp = EnsureComp<PryingComponent>(vampire);
            pryComp.Force = true;
            pryComp.PryPowered = true;

            _popup.PopupEntity(Loc.GetString("vampire-supernaturalstrength", ("user", vampire)), vampire, vampire, Shared.Popups.PopupType.SmallCaution);

            meleeComp.Damage += damage;
        }
    }
    #endregion

    #region Other Powers
    private void Screech(Entity<VampireComponent> vampire, TimeSpan? duration, DamageSpecifier? damage = null)
    {
        foreach (var entity in _entityLookup.GetEntitiesInRange(vampire, 3, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            if (HasComp<BibleUserComponent>(entity))
                continue;

            if (HasComp<VampireComponent>(entity))
                continue;

            if (TryComp<KnockedDownComponent>(entity, out var knockedDownComponent))
            {
                _stun.UpdateKnockdownTime((entity, knockedDownComponent), duration ?? TimeSpan.FromSeconds(3), false);
                _chat.TryEmoteWithoutChat(entity, _prototypeManager.Index<EmotePrototype>(VampireComponent.ScreamEmoteProto), true);
            }

            if (damage != null)
                _damageableSystem.TryChangeDamage(entity, damage);
        }
    }
    private void Glare(Entity<VampireComponent> vampire, EntityUid? target, TimeSpan? duration, DamageSpecifier? damage = null)
    {
        if (!target.HasValue)
            return;

        if (HasComp<VampireComponent>(target))
            return;

        if (HasComp<FlashImmunityComponent>(target))
            return;

        if (HasComp<BibleUserComponent>(target))
        {
            if (TryComp<KnockedDownComponent>(vampire, out var knockdown))
                _stun.UpdateKnockdownTime((vampire, knockdown), duration ?? TimeSpan.FromSeconds(3), true);
            _chat.TryEmoteWithoutChat(vampire.Owner, _prototypeManager.Index<EmotePrototype>(VampireComponent.ScreamEmoteProto), true);
            if (damage != null)
                _damageableSystem.TryChangeDamage(vampire.Owner, damage);
            return;
        }

        if (!TryComp<KnockedDownComponent>(target, out var tgtKnockdown))
            return;

        _stun.UpdateKnockdownTime((target.Value,tgtKnockdown), duration ?? TimeSpan.FromSeconds(3), true);
    }
    private void PolymorphSelf(Entity<VampireComponent> vampire, string? polymorphTarget)
    {
        if (string.IsNullOrEmpty(polymorphTarget))
            return;

        var prototypeId = polymorphTarget switch
        {
            "MobMouse" => "VampireMouse",
            "mobBatVampire" => "VampireBat",
            _ => null
        };

        if (prototypeId == null)
        {
            Logger.Warning($"Unknown polymorph target: {polymorphTarget}. Polymorph operation aborted.");
            return;
        }

        if (!_prototypeManager.TryIndex<PolymorphPrototype>(prototypeId, out var prototype))
        {
            Logger.Warning($"Unknown prototype: {prototypeId}. Polymorph operation aborted.");
            return;
        }

        _polymorph.PolymorphEntity(vampire, prototype);
    }
    private void BloodSteal(Entity<VampireComponent> vampire)
    {
        var transform = Transform(vampire.Owner);

        var targets = new HashSet<(EntityUid, FixedPoint2)>();

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

            var volumeToConsume = (FixedPoint2) Math.Min((float) victimBloodRemaining.Value, 20);

            targets.Add((entity, volumeToConsume));
        }

        if (targets.Count != 0)
        {
            foreach (var (entity, volumeToConsume) in targets)
            {
                if (!TryComp<BloodstreamComponent>(entity, out var bloodstream) || bloodstream.BloodSolution == null)
                    continue;

                //Transfer 80% to the vampire
                var bloodSolution = _solution.SplitSolution(bloodstream.BloodSolution.Value, volumeToConsume * 0.80);
                //And spill 20% on the floor
                _blood.TryModifyBloodLevel(entity, -(volumeToConsume * 0.2));

                //Dont check this time, if we are full - just continue anyway
                TryIngestBlood(vampire, bloodSolution);

                AddBloodEssence(vampire, volumeToConsume * 0.80);

                _beam.TryCreateBeam(vampire, entity, "VampireLightning");

                _popup.PopupEntity(Loc.GetString("vampire-bloodsteal-other"), entity, entity, Shared.Popups.PopupType.LargeCaution);
            }
        }
        else
            _popup.PopupEntity(Loc.GetString("vampire-bloodsteal-no-victims"), vampire, vampire);

        var bloodEssence = _vampire.GetBloodEssence(vampire);

        //Update abilities, add new unlocks
        UpdateAbilities(vampire, vampire.Comp, "ActionVampireBloodSteal", "BloodSteal" , bloodEssence >= FixedPoint2.New(200) && vampire.Comp.CurrentMutation == VampireMutationsType.Hemomancer);
    }
    private bool CloakOfDarkness(Entity<VampireComponent> vampire, float upkeep, float passiveVisibilityRate, float movementVisibilityRate)
    {
        if (HasComp<VampireSealthComponent>(vampire))
        {
            RemComp<StealthOnMoveComponent>(vampire);
            RemComp<StealthComponent>(vampire);
            RemComp<VampireSealthComponent>(vampire);
            _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), vampire, vampire);
            return false;
        }
        else
        {
            EnsureComp<StealthComponent>(vampire);
            var stealthMovement = EnsureComp<StealthOnMoveComponent>(vampire);
            stealthMovement.PassiveVisibilityRate = passiveVisibilityRate;
            stealthMovement.MovementVisibilityRate = movementVisibilityRate;
            var vampireStealth = EnsureComp<VampireSealthComponent>(vampire);
            vampireStealth.Upkeep = upkeep;
            _popup.PopupEntity(Loc.GetString("vampire-cloak-enable"), vampire, vampire);
            return true;
        }
    }
    #endregion

    #region Hypnotise
    private bool TryHypnotise(Entity<VampireComponent> vampire, EntityUid? target, TimeSpan? duration, TimeSpan? delay)
    {
        if (target == null)
            return false;

        var attempt = new FlashAttemptEvent(target.Value, vampire.Owner, vampire.Owner);
        RaiseLocalEvent(target.Value, ref attempt, true);

        if (attempt.Cancelled)
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, delay ?? TimeSpan.FromSeconds(5),
        new VampireHypnotiseDoAfterEvent() { Duration = duration },
        eventTarget: vampire,
        target: target,
        used: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfterEventArgs))
        {
            _popup.PopupEntity(Loc.GetString("vampire-hypnotise-other", ("user", vampire.Owner), ("target", target.Value)), target.Value, Shared.Popups.PopupType.SmallCaution);
        }
        else
        {
            return false;
        }
        return true;
    }
    private void HypnotiseDoAfter(Entity<VampireComponent> vampire, ref VampireHypnotiseDoAfterEvent args)
    {
        if (!args.Target.HasValue)
            return;

        if (args.Cancelled)
            return;

        _statusEffects.TryAddStatusEffectDuration(args.Target.Value, SleepingSystem.StatusEffectForcedSleeping,out var _, args.Duration ?? TimeSpan.FromSeconds(30));
    }
    #endregion

    #region Deaths Embrace
    /// <summary>
    /// When the vampire dies, attempt to activate the Deaths Embrace power
    /// </summary>
    private void OnVampireStateChanged(EntityUid uid, VampireDeathsEmbraceComponent component, MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Dead && args.NewMobState == MobState.Dead)
        {
            //Home still exists?
            TryMoveToCoffin((uid, component));
        }
    }
    /// <summary>
    /// When the vampire is inserted into a container (ie locker, crate etc) check for a coffin, and bind their home to it
    /// </summary>
    private void OnInsertedIntoContainer(EntityUid uid, VampireDeathsEmbraceComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (HasComp<CoffinComponent>(args.Container.Owner))
        {
            component.HomeCoffin = args.Container.Owner;
            var vhComp = EnsureComp<VampireHealingComponent>(args.Entity);
            vhComp.Healing = component.CoffinHealing;
            _popup.PopupEntity(Loc.GetString("vampire-deathsembrace-bind"), uid, uid);
            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(uid):user} bound a new coffin");

        }
    }
    /// <summary>
    /// When leaving a container, remove the healing component
    /// </summary>
    private void OnRemovedFromContainer(EntityUid uid, VampireDeathsEmbraceComponent component, EntGotRemovedFromContainerMessage args)
    {
        RemComp<VampireHealingComponent>(args.Entity);
    }
    /// <summary>
    /// Attempt to move the vampire to their bound coffin
    /// </summary>
    private bool TryMoveToCoffin(Entity<VampireDeathsEmbraceComponent> vampire)
    {
        if (!vampire.Comp.HomeCoffin.HasValue)
            return false;

        //Someone smashed your crib bro'
        if (!Exists(vampire.Comp.HomeCoffin.Value) || LifeStage(vampire.Comp.HomeCoffin.Value) >= EntityLifeStage.Terminating)
        {
            vampire.Comp.HomeCoffin = null;
            return false;
        }

        //Your crib.. is not a crib, how?
        if (!TryComp<EntityStorageComponent>(vampire.Comp.HomeCoffin, out var coffinEntityStorage))
        {
            vampire.Comp.HomeCoffin = null;
            return false;
        }

        //I guess its full?
        if (!_entityStorage.CanInsert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage))
            return false;

        Spawn("Smoke", Transform(vampire).Coordinates);

        _entityStorage.CloseStorage(vampire.Comp.HomeCoffin.Value, coffinEntityStorage);

        if (_entityStorage.Insert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage))
        {
            _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(vampire):user} was moved to their home coffin");
            return true;
        }

        return false;
    }
    /// <summary>
    /// Heal the vampire while they are in the coffin
    /// Revive them if they are dead and below a ~150ish damage
    /// </summary>
    private void DoCoffinHeal(EntityUid vampire, VampireHealingComponent healing)
    {
        if (healing.Healing == null)
            return;

        _damageableSystem.TryChangeDamage(vampire, healing.Healing, true, origin: vampire);

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
            _mobState.ChangeMobState(vampire, MobState.Critical, mobStateComponent, vampire);
        }
    }
    #endregion

    #region Blood Drinking
    /// <summary>
    /// Toggle if fangs are extended
    /// </summary>
    private bool ToggleFangs(Entity<VampireComponent> vampire)
    {
        if (HasComp<VampireFangsExtendedComponent>(vampire))
        {
            RemComp<VampireFangsExtendedComponent>(vampire);
            var popupText = Loc.GetString("vampire-fangs-retracted");
            _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(vampire):user} retracted their fangs");
            _popup.PopupEntity(popupText, vampire.Owner, vampire.Owner);
            return false;
        }
        else
        {
            EnsureComp<VampireFangsExtendedComponent>(vampire);
            var popupText = Loc.GetString("vampire-fangs-extended");
            _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(vampire):user} extended their fangs");
            _popup.PopupEntity(popupText, vampire.Owner, vampire.Owner);
            return true;
        }
    }
    /// <summary>
    /// Check and start drinking blood from a humanoid
    /// </summary>
    private bool TryDrink(Entity<VampireComponent> vampire, EntityUid target, TimeSpan doAfterDelay)
    {
        //Do a precheck
        if (!HasComp<VampireFangsExtendedComponent>(vampire))
            return false;

        if (!HasComp<TransformComponent>(vampire))
            return false;

        if (!_interaction.InRangeUnobstructed(vampire.Owner, target, popup: true))
            return false;

        if (!_ingestion.HasMouthAvailable(vampire, target))
            return false;

        if (_rotting.IsRotten(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), vampire, vampire, PopupType.SmallCaution);
            return false;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, doAfterDelay,
        new VampireDrinkBloodDoAfterEvent() { Volume = vampire.Comp.MouthVolume },
        eventTarget: vampire,
        target: target,
        used: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            NeedHand = false,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
    private void DrinkDoAfter(Entity<VampireComponent> entity, ref VampireDrinkBloodDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<VampireFangsExtendedComponent>(entity))
            return;

        if (!_ingestion.HasMouthAvailable(entity, entity))
            return;

        if (_rotting.IsRotten(args.Target!.Value))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryComp<BloodstreamComponent>(args.Target, out var targetBloodstream) || targetBloodstream == null || targetBloodstream.BloodSolution == null)
            return;

        //Ensure there is enough blood to drain
        var victimBloodRemaining = targetBloodstream.BloodSolution.Value.Comp.Solution.Volume;
        if (victimBloodRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), entity.Owner, entity.Owner, PopupType.SmallCaution);
            return;
        }

        var volumeToConsume = (FixedPoint2) Math.Min((float) victimBloodRemaining.Value, args.Volume);
        var volumeToDrain = (FixedPoint2) Math.Min((float) victimBloodRemaining.Value, args.Volume * 8);

        if (_mind.TryGetMind(entity, out var mindId, out var mind))
            if (_mind.TryGetObjectiveComp<BloodDrainConditionComponent>(mindId, out var objective, mind))
                    objective.BloodDranked = entity.Comp.TotalBloodDrank;

        //Slurp
        _audio.PlayPvs(entity.Comp.BloodDrainSound, entity.Owner, AudioParams.Default.WithVolume(-3f));

        //Spill an extra 5% on the floor
        _blood.TryModifyBloodLevel(args.Target.Value, -(volumeToDrain * 0.05));

        //Thou shall not feed upon the blood of the holy
        //TODO: Replace with raised event?
        if (HasComp<BibleUserComponent>(args.Target))
        {
            _damageableSystem.TryChangeDamage(entity, VampireComponent.HolyDamage, true);
            _popup.PopupEntity(Loc.GetString("vampire-ingest-holyblood"), entity, entity, PopupType.LargeCaution);
            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(entity):user} attempted to drink {volumeToConsume}u of {ToPrettyString(args.Target):target}'s holy blood");
            return;
        }
        //Check for zombie
        else
        {
            //Pull out some of the blood
            var bloodSolution = _solution.SplitSolution(targetBloodstream.BloodSolution.Value, volumeToConsume);

            if (!TryIngestBlood(entity, bloodSolution))
            {
                //Undo, put the blood back
                _solution.AddSolution(targetBloodstream.BloodSolution.Value, bloodSolution);
                return;
            }

            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(entity):user} drank {volumeToConsume}u of {ToPrettyString(args.Target):target}'s blood");
            AddBloodEssence(entity, volumeToConsume * 0.95);

            args.Repeat = true;
        }
    }
    /// <summary>
    /// Attempt to insert the solution into the first stomach that has space available
    /// </summary>
    private bool TryIngestBlood(Entity<VampireComponent> vampire, Solution ingestedSolution, bool force = false)
    {
        //Get all stomaches
        if (TryComp<BodyComponent>(vampire.Owner, out var body) && _body.TryGetBodyOrganEntityComps<StomachComponent>((vampire.Owner, body), out var stomachs))
        {
            //Pick the first one that has space available
            var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Owner, ingestedSolution, stomach.Comp1));
            if (firstStomach == null)
            {
                //We are full
                _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), vampire.Owner, vampire.Owner, PopupType.SmallCaution);
                return false;
            }
            //Fill the stomach with that delicious blood
            return _stomach.TryTransferSolution(firstStomach.Value.Owner, ingestedSolution, firstStomach.Value.Comp1);
        }

        //No stomach
        return false;
    }
    #endregion

    private bool IsPowerUnlocked(VampireComponent vampire, string name)
    {
        return vampire.UnlockedPowers.ContainsKey(name);
    }
    /*private bool IsPowerActive(VampireComponent vampire, VampirePowerProtype def) => IsPowerActive(vampire, def.ID);
    private bool IsPowerActive(VampireComponent vampire, string name)
    {
        return vampire.ActivePowers.Contains(name);
    }
    private bool SetPowerActive(VampireComponent vampire, string name, bool active)
    {
        if (active)
        {
            return vampire.ActivePowers.Add(name);
        }
        else
        {
            return vampire.ActivePowers.Remove(name);
        }
    }*/
    /// <summary>
    /// Gets the Action EntityUid for a specific power
    /// </summary>
    private EntityUid? GetPowerEntity(VampireComponent vampire, string name)
    {
        if (!vampire.UnlockedPowers.TryGetValue(name, out var ability))
            return null;

        return GetEntity(ability);
    }

    /// <summary>
    /// Cache all power prototypes in a dictionary by keyed by ID
    /// </summary>
    /// <returns></returns>
    private FrozenDictionary<string, VampirePowerProtype> BuildPowerCache()
    {
        var protos = _prototypeManager.EnumeratePrototypes<VampirePowerProtype>();
        return protos.ToFrozenDictionary(x => x.ID);
    }

    /// <summary>
    /// Cache all passive prototypes in a dictionary by keyed by listing id
    /// </summary>
    /// <returns></returns>
    private FrozenDictionary<string, VampirePassiveProtype> BuildPassiveCache()
    {
        var protos = _prototypeManager.EnumeratePrototypes<VampirePassiveProtype>();
        return protos.ToFrozenDictionary(x => x.CatalogEntry);
    }
}

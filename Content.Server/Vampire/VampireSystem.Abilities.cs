using Content.Server.Bible.Components;
using Content.Server.Body.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Store.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    /// <summary>
    /// Upon using any power that does not require a target
    /// </summary>
    private void OnUseSelfPower(EntityUid uid, VampireComponent component, VampireSelfPowerEvent args) => TriggerPower((uid, component), args.Type, args.Details);
    /// <summary>
    /// Upon using any power that requires a target
    /// </summary>
    private void OnUseTargetedPower(EntityUid uid, VampireComponent component, VampireTargetedPowerEvent args) => TriggerPower((uid, component), args.Type, args.Details, args.Target);
    private void TriggerPower(Entity<VampireComponent> vampire, VampirePowerKey powerType, VampirePowerDetails def, EntityUid? target = null)
    {
        if (!IsAbilityUnlocked(vampire, powerType))
            return;

        if (def.ActivationCost > 0 && def.ActivationCost > GetBloodEssence(vampire))
        {
            _popup.PopupClient(Loc.GetString("vampire-not-enough-blood"), vampire, vampire, PopupType.MediumCaution);
            return;
        }

        //Block if we are cuffed
        if (!def.UsableWhileCuffed && TryComp<CuffableComponent>(vampire, out var cuffable) && !cuffable.CanStillInteract)
        {
            _popup.PopupEntity(Loc.GetString("vampire-cuffed"), vampire, vampire, PopupType.MediumCaution);
            return;
        }

        //Block if we are stunned
        if (!def.UsableWhileStunned && HasComp<StunnedComponent>(vampire))
        {
            _popup.PopupEntity(Loc.GetString("vampire-stunned"), vampire, vampire, PopupType.MediumCaution);
            return;
        }

        //Block if we are muzzled - so far only one item does this?
        if (!def.UsableWhileMuffled && TryComp<ReplacementAccentComponent>(vampire, out var accent) && accent.Accent.Equals("mumble"))
        {
            _popup.PopupEntity(Loc.GetString("vampire-muffled"), vampire, vampire, PopupType.MediumCaution);
            return;
        }

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
            case VampirePowerKey.Polymorph:
                {
                    PolymorphSelf(vampire, def.PolymorphTarget);
                    break;
                }
            case VampirePowerKey.Hypnotise:
                {
                    success = TryHypnotise(vampire, target, def.Duration, def.DoAfterDelay);
                    break;
                }
            case VampirePowerKey.BloodSteal:
                {
                    BloodSteal(vampire);
                    break;
                }
            case VampirePowerKey.CloakOfDarkness:
                {
                    CloakOfDarkness(vampire, def.Upkeep, -1, 1);
                    break;
                }
            default:
                break;
        }

        if (!success)
            return;

        AddBloodEssence(vampire, -def.ActivationCost);

        _action.StartUseDelay(GetAbilityEntity(vampire, powerType));
    }

    #region Other Powers
    /// <summary>
    /// Spawn and bind the pendant if one does not already exist, otherwise just summon to the vampires hand
    /// </summary>
    private void OnSummonHeirloom(EntityUid uid, VampireComponent component, VampireSummonHeirloomEvent args)
    {
        if (!component.Heirloom.HasValue
            || LifeStage(component.Heirloom.Value) >= EntityLifeStage.Terminating)
        {
            //If the pendant does not exist, or has been deleted - spawn one
            component.Heirloom = Spawn(VampireComponent.HeirloomProto);

            if (TryComp<VampireHeirloomComponent>(component.Heirloom, out var heirloomComponent))
                heirloomComponent.Owner = uid;

            if (TryComp<StoreComponent>(component.Heirloom, out var storeComponent))
                //Tie the store balance to the vampires balance
                storeComponent.Balance = component.Balance;
        }
        //Move to players hands
        _hands.PickupOrDrop(uid, component.Heirloom.Value);
    }
    private void Screech(Entity<VampireComponent> vampire, TimeSpan? duration, DamageSpecifier? damage = null)
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
                _stun.TryParalyze(entity, duration ?? TimeSpan.FromSeconds(3), false);
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

        if (!HasComp<FlashableComponent>(target))
            return;

        if (HasComp<FlashImmunityComponent>(target))
            return;

        if (HasComp<BibleUserComponent>(target))
        {
            _stun.TryParalyze(vampire, duration ?? TimeSpan.FromSeconds(3), true);
            _chat.TryEmoteWithoutChat(vampire.Owner, _prototypeManager.Index<EmotePrototype>(VampireComponent.ScreamEmoteProto), true);
            if (damage != null)
                _damageableSystem.TryChangeDamage(vampire.Owner, damage);
            return;
        }

        _stun.TryParalyze(target.Value, duration ?? TimeSpan.FromSeconds(3), true);
    }
    private void PolymorphSelf(Entity<VampireComponent> vampire, string? polymorphTarget)
    {
        if (polymorphTarget == null)
            return;

        _polymorph.PolymorphEntity(vampire, polymorphTarget);
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

            AddBloodEssence(vampire, volumeToConsume * 0.80);

            _beam.TryCreateBeam(vampire, entity, "Lightning");

            _popup.PopupEntity(Loc.GetString("vampire-bloodsteal-other"), entity, entity, Shared.Popups.PopupType.LargeCaution);
        }



        //Update abilities, add new unlocks
        //UpdateAbilities(vampire);
    }
    private void CloakOfDarkness(Entity<VampireComponent> vampire, float upkeep, float passiveVisibilityRate, float movementVisibilityRate)
    {
        var actionEntity = GetAbilityEntity(vampire.Comp, VampirePowerKey.CloakOfDarkness);
        if (IsAbilityActive(vampire.Comp, VampirePowerKey.CloakOfDarkness))
        {
            SetAbilityActive(vampire.Comp, VampirePowerKey.CloakOfDarkness, false);
            _action.SetToggled(actionEntity, false);
            RemComp<StealthOnMoveComponent>(vampire);
            RemComp<StealthComponent>(vampire);
            RemComp<VampireSealthComponent>(vampire);
            _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), vampire, vampire);
        }
        else
        {
            SetAbilityActive(vampire.Comp, VampirePowerKey.CloakOfDarkness, true);
            _action.SetToggled(actionEntity, true);
            EnsureComp<StealthComponent>(vampire);
            var stealthMovement = EnsureComp<StealthOnMoveComponent>(vampire);
            stealthMovement.PassiveVisibilityRate = passiveVisibilityRate;
            stealthMovement.MovementVisibilityRate = movementVisibilityRate;
            var vampireStealth = EnsureComp<VampireSealthComponent>(vampire);
            vampireStealth.Upkeep = upkeep;
            _popup.PopupEntity(Loc.GetString("vampire-cloak-enable"), vampire, vampire);
        }
    }
    #endregion

    #region Hypnotise
    private bool TryHypnotise(Entity<VampireComponent> vampire, EntityUid? target, TimeSpan? duration, TimeSpan? delay)
    {
        if (target == null)
            return false;

        var attempt = new FlashAttemptEvent(target.Value, vampire.Owner, vampire.Owner);
        RaiseLocalEvent(target.Value, attempt, true);

        if (attempt.Cancelled)
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, delay ?? TimeSpan.FromSeconds(5),
        new VampireHypnotiseEvent() { Duration = duration },
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

        _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(args.Target.Value, VampireComponent.SleepStatusEffectProto, args.Duration ?? TimeSpan.FromSeconds(30), false);
    }
    #endregion

    #region Deaths Embrace
    /// <summary>
    /// When the vampire dies, attempt to activate the Deaths Embrace power
    /// </summary>
    private void OnVampireStateChanged(EntityUid uid, VampireComponent component, MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Dead && args.NewMobState == MobState.Dead)
            OnUseSelfPower(uid, component, new() { Type = VampirePowerKey.DeathsEmbrace, Details = new() { ActivationCost = 100 } });
    }
    /// <summary>
    /// When the vampire is inserted into a container (ie locker, crate etc) check for a coffin, and bind their home to it
    /// </summary>
    private void OnInsertedIntoContainer(EntityUid uid, VampireComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (HasComp<CoffinComponent>(args.Container.Owner))
        {
            component.HomeCoffin = args.Container.Owner;
            EnsureComp<VampireHealingComponent>(args.Entity);
            _popup.PopupEntity(Loc.GetString("vampire-deathsembrace-bind"), uid, uid);
        }
    }
    /// <summary>
    /// When leaving a container, remove the healing component
    /// </summary>
    private void OnRemovedFromContainer(EntityUid uid, VampireComponent component, EntGotRemovedFromContainerMessage args)
    {
        //Presence check is done upstream
        RemComp<VampireHealingComponent>(args.Entity);
    }
    /// <summary>
    /// Attempt to move the vampire to their bound coffin
    /// </summary>
    private bool TryMoveToCoffin(Entity<VampireComponent> vampire)
    {
        if (!vampire.Comp.HomeCoffin.HasValue)
            return false;

        //Someone smashed your crib bro'
        if (!Exists(vampire.Comp.HomeCoffin.Value)
            || LifeStage(vampire.Comp.HomeCoffin.Value) == EntityLifeStage.Terminating
            || LifeStage(vampire.Comp.HomeCoffin.Value) == EntityLifeStage.Deleted)
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

        return _entityStorage.Insert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage);
    }
    /// <summary>
    /// Heal the vampire while they are in the coffin
    /// Revive them if they are dead and below a ~150ish damage
    /// </summary>
    private void DoCoffinHeal(EntityUid vampire, VampireHealingComponent healing)
    {
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
    /// Check and start drinking blood from a humanoid
    /// </summary>
    private void OnInteractWithHumanoid(EntityUid uid, HumanoidAppearanceComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;

        if (!TryComp<VampireComponent>(args.User, out var vampireComponent))
            return;

        args.Handled = TryDrink((args.User, vampireComponent), args.Target, TimeSpan.FromSeconds(1));
    }
    private void ToggleFangs(Entity<VampireComponent> vampire)
    {
        var actionEntity = GetAbilityEntity(vampire.Comp, VampirePowerKey.ToggleFangs);
        var popupText = string.Empty;
        if (IsAbilityActive(vampire, VampirePowerKey.ToggleFangs))
        {
            SetAbilityActive(vampire.Comp, VampirePowerKey.ToggleFangs, false);
            _action.SetToggled(actionEntity, false);
            popupText = Loc.GetString("vampire-fangs-retracted");
        }
        else
        {
            SetAbilityActive(vampire.Comp, VampirePowerKey.ToggleFangs, true);
            _action.SetToggled(vampire.Comp.UnlockedPowers[VampirePowerKey.ToggleFangs], true);
            popupText = Loc.GetString("vampire-fangs-extended");
        }
        _popup.PopupEntity(popupText, vampire.Owner, vampire.Owner);
    }

    private bool TryDrink(Entity<VampireComponent> vampire, EntityUid target, TimeSpan doAfterDelay)
    {
        //Do a precheck
        if (!IsAbilityActive(vampire.Comp, VampirePowerKey.ToggleFangs))
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

        var doAfterEventArgs = new DoAfterArgs(EntityManager, vampire, doAfterDelay,
        new VampireDrinkBloodEvent() { Volume = 5 },
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
        if (args.Cancelled)
            return;

        if (!args.Target.HasValue)
            return;

        if (!IsAbilityActive(entity.Comp, VampirePowerKey.ToggleFangs))
            return;

        if (_food.IsMouthBlocked(args.Target.Value, entity))
            return;

        //Thou shall not feed upon the blood of the holy
        if (HasComp<BibleUserComponent>(args.Target))
        {
            _damageableSystem.TryChangeDamage(entity, VampireComponent.HolyDamage, true);
            _popup.PopupEntity(Loc.GetString("vampire-ingest-holyblood"), entity, entity, PopupType.LargeCaution);
            args.Repeat = false;
        }

        if (_rotting.IsRotten(args.Target.Value))
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

        args.Repeat = true;

        //Transfer 95% to the vampire
        var bloodSolution = _solution.SplitSolution(targetBloodstream.BloodSolution.Value, volumeToConsume * 0.95);

        if (!TryIngestBlood(entity, bloodSolution))
        {
            //Undo, put the blood back
            _solution.AddSolution(targetBloodstream.BloodSolution.Value, bloodSolution);
            args.Repeat = false;
            return;
        }

        //Slurp
        _audio.PlayPvs(entity.Comp.BloodDrainSound, entity.Owner, AudioParams.Default.WithVolume(-3f));

        AddBloodEssence(entity, volumeToConsume * 0.95);

        //And spill 5% on the floor
        _blood.TryModifyBloodLevel(args.Target.Value, -(volumeToConsume * 0.05));
    }
    /// <summary>
    /// Attempt to insert the solution into the first stomach that has space available
    /// </summary>
    private bool TryIngestBlood(Entity<VampireComponent> vampire, Solution ingestedSolution, bool force = false)
    {
        //Get all stomaches
        if (TryComp<BodyComponent>(vampire.Owner, out var body) && _body.TryGetBodyOrganComponents<StomachComponent>(vampire.Owner, out var stomachs, body))
        {
            //Pick the first one that has space available
            var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, ingestedSolution, stomach.Comp));
            if (firstStomach == null)
            {
                //We are full
                _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), vampire.Owner, vampire.Owner, PopupType.SmallCaution);
                return false;
            }
            //Fill the stomach with that delicious blood
            return _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, ingestedSolution, firstStomach.Value.Comp);
        }

        //No stomach
        return false;
    }
    #endregion
}

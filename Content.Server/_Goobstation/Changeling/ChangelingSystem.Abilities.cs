using Content.Shared.Changeling;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Store.Components;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Atmos.Rotting;
using Content.Server.Objectives.Components;
using Content.Server.Light.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.Flash.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Damage.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Mindshield.Components;
using Content.Shared._Goobstation.FakeMindshield.Components;

namespace Content.Server.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, OpenEvolutionMenuEvent>(OnOpenEvolutionMenu);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDNAEvent>(OnAbsorb);
        SubscribeLocalEvent<ChangelingComponent, AbsorbDNADoAfterEvent>(OnAbsorbDoAfter);
        SubscribeLocalEvent<ChangelingComponent, StingExtractDNAEvent>(OnStingExtractDNA);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformCycleEvent>(OnTransformCycle);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformEvent>(OnTransform);
        SubscribeLocalEvent<ChangelingComponent, EnterStasisEvent>(OnEnterStasis);
        SubscribeLocalEvent<ChangelingComponent, ExitStasisEvent>(OnExitStasis);

        SubscribeLocalEvent<ChangelingComponent, ToggleArmbladeEvent>(OnToggleArmblade);
        SubscribeLocalEvent<ChangelingComponent, CreateBoneShardEvent>(OnCreateBoneShard);
        SubscribeLocalEvent<ChangelingComponent, ToggleChitinousArmorEvent>(OnToggleArmor);
        SubscribeLocalEvent<ChangelingComponent, ToggleOrganicShieldEvent>(OnToggleShield);
        SubscribeLocalEvent<ChangelingComponent, ShriekDissonantEvent>(OnShriekDissonant);
        SubscribeLocalEvent<ChangelingComponent, ShriekResonantEvent>(OnShriekResonant);
        SubscribeLocalEvent<ChangelingComponent, ToggleStrainedMusclesEvent>(OnToggleStrainedMuscles);

        SubscribeLocalEvent<ChangelingComponent, StingBlindEvent>(OnStingBlind);
        SubscribeLocalEvent<ChangelingComponent, StingCryoEvent>(OnStingCryo);
        SubscribeLocalEvent<ChangelingComponent, StingLethargicEvent>(OnStingLethargic);
        SubscribeLocalEvent<ChangelingComponent, StingMuteEvent>(OnStingMute);
        SubscribeLocalEvent<ChangelingComponent, StingTransformEvent>(OnStingTransform);
        SubscribeLocalEvent<ChangelingComponent, StingFakeArmbladeEvent>(OnStingFakeArmblade);
        SubscribeLocalEvent<ChangelingComponent, StingLayEggsEvent>(OnLayEgg);

        SubscribeLocalEvent<ChangelingComponent, ActionAnatomicPanaceaEvent>(OnAnatomicPanacea);
        SubscribeLocalEvent<ChangelingComponent, ActionAugmentedEyesightEvent>(OnAugmentedEyesight);
        SubscribeLocalEvent<ChangelingComponent, ActionBiodegradeEvent>(OnBiodegrade);
        SubscribeLocalEvent<ChangelingComponent, ActionChameleonSkinEvent>(OnChameleonSkin);
        SubscribeLocalEvent<ChangelingComponent, ActionEphedrineOverdoseEvent>(OnEphedrineOverdose);
        SubscribeLocalEvent<ChangelingComponent, ActionFleshmendEvent>(OnHealUltraSwag);
        SubscribeLocalEvent<ChangelingComponent, ActionLastResortEvent>(OnLastResort);
        SubscribeLocalEvent<ChangelingComponent, ActionLesserFormEvent>(OnLesserForm);
        SubscribeLocalEvent<ChangelingComponent, ActionMindshieldFakeEvent>(OnMindshieldFake);
        SubscribeLocalEvent<ChangelingComponent, ActionSpacesuitEvent>(OnSpacesuit);
        SubscribeLocalEvent<ChangelingComponent, ActionHivemindAccessEvent>(OnHivemindAccess);
    }

    #region Basic Abilities

    private void OnOpenEvolutionMenu(EntityUid uid, ChangelingComponent comp, ref OpenEvolutionMenuEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnAbsorb(EntityUid uid, ChangelingComponent comp, ref AbsorbDNAEvent args)
    {
        var target = args.Target;

        if (!IsIncapacitated(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-incapacitated", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }
        if (HasComp<AbsorbedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-absorbed", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }
        if (!HasComp<AbsorbableComponent>(target) || (TryComp<AbsorbableComponent>(target, out var absorbComp) && absorbComp.Disabled))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-unabsorbable", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }
        if (TryComp<RottingComponent>(target, out var rotComp) && _rotting.RotStage(target, rotComp) >= 2)
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-extremely-bloated", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, comp, args))
            return;

        var popupSelf = Loc.GetString("changeling-absorb-start-self", ("target", Identity.Entity(target, EntityManager)));
        var popupTarget = Loc.GetString("changeling-absorb-start-target");
        var popupOthers = Loc.GetString("changeling-absorb-start-others", ("user", Identity.Entity(uid, EntityManager)), ("target", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(popupSelf, uid, uid);
        _popup.PopupEntity(popupTarget, target, target, PopupType.MediumCaution);
        _popup.PopupEntity(popupOthers, uid, Filter.Pvs(uid).RemovePlayersByAttachedEntity([uid, target]), true, PopupType.MediumCaution);

        PlayMeatySound(uid, comp);
        var dargs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(15), new AbsorbDNADoAfterEvent(), uid, target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(dargs);
    }
    public ProtoId<DamageGroupPrototype> AbsorbedDamageGroup = "Genetic";
    private void OnAbsorbDoAfter(EntityUid uid, ChangelingComponent comp, ref AbsorbDNADoAfterEvent args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (args.Cancelled || !IsIncapacitated(target) || HasComp<AbsorbedComponent>(target))
            return;

        PlayMeatySound(args.User, comp);

        var reducedBiomass = false;
        if (HasComp<RottingComponent>(target) || (TryComp<AbsorbableComponent>(target, out var absorbComp) && absorbComp.ReducedBiomass))
            reducedBiomass = true;

        float biomassModifier = 1f;
        if (reducedBiomass)
            biomassModifier = 0.5f;

        UpdateBiomass(uid, comp, (comp.MaxBiomass * biomassModifier) - comp.TotalAbsorbedEntities);

        var dmg = new DamageSpecifier(_proto.Index(AbsorbedDamageGroup), 200);
        _damage.TryChangeDamage(target, dmg, true, false);
        _blood.ChangeBloodReagent(target, "FerrochromicAcid");
        _blood.SpillAllSolutions(target);

        EnsureComp<AbsorbedComponent>(target);

        var popupSelf = Loc.GetString("changeling-absorb-end-self", ("target", Identity.Entity(target, EntityManager)));
        var popupTarget = Loc.GetString("changeling-absorb-end-target");
        var popupOthers = Loc.GetString("changeling-absorb-end-others", ("user", Identity.Entity(uid, EntityManager)), ("target", Identity.Entity(target, EntityManager)));

        var bonusChemicals = 0f;
        var bonusEvolutionPoints = 0;

        if (TryComp<ChangelingComponent>(target, out var targetComp))
        {
            popupSelf = Loc.GetString("changeling-absorb-end-self-ling", ("target", Identity.Entity(target, EntityManager)));
            bonusChemicals += targetComp.MaxChemicals / 2;
            bonusEvolutionPoints += 2;
            comp.MaxBiomass += targetComp.MaxBiomass / 2;
        }
        else
        {
            bonusChemicals += 10;

            if (reducedBiomass)
                popupSelf = Loc.GetString("changeling-absorb-end-self-reduced-biomass", ("target", Identity.Entity(target, EntityManager)));
        }

        _popup.PopupEntity(popupSelf, uid, uid);
        _popup.PopupEntity(popupTarget, target, target, PopupType.LargeCaution);
        _popup.PopupEntity(popupOthers, uid, Filter.Pvs(uid).RemovePlayersByAttachedEntity([uid, target]), true, PopupType.LargeCaution);

        TryStealDNA(uid, target, comp, true);
        comp.TotalAbsorbedEntities++;
        comp.MaxChemicals += bonusChemicals;
        comp.MaxEvolutionPoints += bonusEvolutionPoints;

        if (TryComp<StoreComponent>(args.User, out var store))
        {
            _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "EvolutionPoint", bonusEvolutionPoints } }, args.User, store);
            _store.UpdateUserInterface(args.User, args.User, store);
            _store.EnableRefund(uid, args.User, store);
        }

        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            if (_mind.TryGetObjectiveComp<AbsorbConditionComponent>(mindId, out var objective, mind))
                objective.Absorbed += 1;
    }

    private void OnStingExtractDNA(EntityUid uid, ChangelingComponent comp, ref StingExtractDNAEvent args)
    {
        if (!TrySting(uid, comp, args, true))
            return;

        var target = args.Target;
        if (!TryStealDNA(uid, target, comp, true))
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            // _popup.PopupEntity(Loc.GetString("changeling-sting-target"), target, target);
        }
    }

    private void OnTransformCycle(EntityUid uid, ChangelingComponent comp, ref ChangelingTransformCycleEvent args)
    {
        comp.AbsorbedDNAIndex += 1;
        if (comp.AbsorbedDNAIndex >= comp.MaxAbsorbedDNA || comp.AbsorbedDNAIndex >= comp.AbsorbedDNA.Count)
            comp.AbsorbedDNAIndex = 0;

        if (comp.AbsorbedDNA.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transform-cycle-empty"), uid, uid);
            return;
        }

        var selected = comp.AbsorbedDNA.ToArray()[comp.AbsorbedDNAIndex];
        comp.SelectedForm = selected;
        _popup.PopupEntity(Loc.GetString("changeling-transform-cycle", ("target", selected.Name)), uid, uid);
    }
    private void OnTransform(EntityUid uid, ChangelingComponent comp, ref ChangelingTransformEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryTransform(uid, comp))
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
    }

    private void OnEnterStasis(EntityUid uid, ChangelingComponent comp, ref EnterStasisEvent args)
    {
        if (comp.IsInStasis || HasComp<AbsorbedComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-stasis-enter-fail"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, comp, args))
            return;

        comp.Chemicals = 0f;

        if (_mobState.IsAlive(uid))
        {
            // fake our death
            var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", uid));
            _popup.PopupEntity(othersMessage, uid, Robust.Shared.Player.Filter.PvsExcept(uid), true);

            var selfMessage = Loc.GetString("changeling-stasis-enter");
            _popup.PopupEntity(selfMessage, uid, uid);
        }

        if (!_mobState.IsDead(uid))
            _mobState.ChangeMobState(uid, MobState.Dead);

        comp.IsInStasis = true;
    }
    private void OnExitStasis(EntityUid uid, ChangelingComponent comp, ref ExitStasisEvent args)
    {
        if (!comp.IsInStasis)
        {
            _popup.PopupEntity(Loc.GetString("changeling-stasis-exit-fail"), uid, uid);
            return;
        }
        if (HasComp<AbsorbedComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-stasis-exit-fail-dead"), uid, uid);
            return;
        }

        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        // heal of everything
        _damage.SetAllDamage(uid, damageable, 0);
        _mobState.ChangeMobState(uid, MobState.Alive);
        _blood.TryModifyBloodLevel(uid, 1000);
        _blood.TryModifyBleedAmount(uid, -1000);

        _popup.PopupEntity(Loc.GetString("changeling-stasis-exit"), uid, uid);

        comp.IsInStasis = false;
    }

    #endregion

    #region Combat Abilities

    private void OnToggleArmblade(EntityUid uid, ChangelingComponent comp, ref ToggleArmbladeEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleItem(uid, ArmbladePrototype, comp))
            return;

        PlayMeatySound(uid, comp);
    }
    private void OnCreateBoneShard(EntityUid uid, ChangelingComponent comp, ref CreateBoneShardEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        var star = Spawn(BoneShardPrototype, Transform(uid).Coordinates);
        _hands.TryPickupAnyHand(uid, star);

        PlayMeatySound(uid, comp);
    }
    private void OnToggleArmor(EntityUid uid, ChangelingComponent comp, ref ToggleChitinousArmorEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleArmor(uid, comp, [(ArmorHelmetPrototype, "head"), (ArmorPrototype, "outerClothing")]))
        {
            _popup.PopupEntity(Loc.GetString("changeling-equip-armor-fail"), uid, uid);
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            return;
        }

        PlayMeatySound(uid, comp);
    }
    private void OnToggleShield(EntityUid uid, ChangelingComponent comp, ref ToggleOrganicShieldEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleItem(uid, ShieldPrototype, comp))
            return;

        PlayMeatySound(uid, comp);
    }
    private void OnShriekDissonant(EntityUid uid, ChangelingComponent comp, ref ShriekDissonantEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        DoScreech(uid, comp);

        var pos = _transform.GetMapCoordinates(uid);
        var power = comp.ShriekPower;
        _emp.EmpPulse(pos, power, 5000f, power * 2);
    }
    private void OnShriekResonant(EntityUid uid, ChangelingComponent comp, ref ShriekResonantEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        DoScreech(uid, comp);

        var power = comp.ShriekPower;
        _flash.FlashArea(uid, uid, power, power * 2f * 1000f);

        var lookup = _lookup.GetEntitiesInRange(uid, power);
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup)
            if (lights.HasComponent(ent))
                _light.TryDestroyBulb(ent);
    }
    private void OnToggleStrainedMuscles(EntityUid uid, ChangelingComponent comp, ref ToggleStrainedMusclesEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        ToggleStrainedMuscles(uid, comp);
    }
    private void ToggleStrainedMuscles(EntityUid uid, ChangelingComponent comp)
    {
        if (!comp.StrainedMusclesActive)
        {
            _popup.PopupEntity(Loc.GetString("changeling-muscles-start"), uid, uid);
            comp.StrainedMusclesActive = true;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-muscles-end"), uid, uid);
            comp.StrainedMusclesActive = false;
        }

        PlayMeatySound(uid, comp);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    #endregion

    #region Stings

    private void OnStingBlind(EntityUid uid, ChangelingComponent comp, ref StingBlindEvent args)
    {
        if (!TrySting(uid, comp, args))
            return;

        var target = args.Target;
        if (!TryComp<BlindableComponent>(target, out var blindable) || blindable.IsBlind)
            return;

        _blindable.AdjustEyeDamage((target, blindable), 2);
        var timeSpan = TimeSpan.FromSeconds(5f);
        _statusEffect.TryAddStatusEffect(target, TemporaryBlindnessSystem.BlindingStatusEffect, timeSpan, false, TemporaryBlindnessSystem.BlindingStatusEffect);
    }
    private void OnStingCryo(EntityUid uid, ChangelingComponent comp, ref StingCryoEvent args)
    {
        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Fresium", 20f)
        };

        if (!TryReagentSting(uid, comp, args, reagents))
            return;
    }
    private void OnStingLethargic(EntityUid uid, ChangelingComponent comp, ref StingLethargicEvent args)
    {
        var reagents = new List<(string, FixedPoint2)>()
        {
            ("ChloralHydrate", 15f)
        };

        if (!TryReagentSting(uid, comp, args, reagents))
            return;
    }
    private void OnStingMute(EntityUid uid, ChangelingComponent comp, ref StingMuteEvent args)
    {
        var reagents = new List<(string, FixedPoint2)>()
        {
            ("MuteToxin", 9f)
        };

        if (!TryReagentSting(uid, comp, args, reagents))
            return;
    }
    private void OnStingTransform(EntityUid uid, ChangelingComponent comp, ref StingTransformEvent args)
    {
        if (!TrySting(uid, comp, args, true))
            return;

        var target = args.Target;
        if (!TryTransform(target, comp, true, true))
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
    }
    private void OnStingFakeArmblade(EntityUid uid, ChangelingComponent comp, ref StingFakeArmbladeEvent args)
    {
        if (!TrySting(uid, comp, args))
            return;

        var target = args.Target;
        var fakeArmblade = EntityManager.SpawnEntity(FakeArmbladePrototype, Transform(target).Coordinates);
        if (!_hands.TryPickupAnyHand(target, fakeArmblade))
        {
            QueueDel(fakeArmblade);
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            _popup.PopupEntity(Loc.GetString("changeling-sting-fail-simplemob"), uid, uid);
            return;
        }

        PlayMeatySound(target, comp);
    }
    public void OnLayEgg(EntityUid uid, ChangelingComponent comp, ref StingLayEggsEvent args)
    {
        var target = args.Target;

        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-incapacitated", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }
        if (HasComp<AbsorbedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-absorbed", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }
        if (!HasComp<AbsorbableComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-absorb-fail-unabsorbable", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        var mind = _mind.GetMind(uid);
        if (mind == null)
            return;
        if (!TryComp<StoreComponent>(uid, out var storeComp))
            return;

        comp.IsInLastResort = false;
        comp.IsInLesserForm = true;

        var eggComp = EnsureComp<ChangelingEggComponent>(target);
        eggComp.lingComp = comp;
        eggComp.lingMind = (EntityUid) mind;
        eggComp.lingStore = _serialization.CreateCopy(storeComp, notNullableOverride: true);

        EnsureComp<AbsorbedComponent>(target);
        var dmg = new DamageSpecifier(_proto.Index(AbsorbedDamageGroup), 200);
        _damage.TryChangeDamage(target, dmg, true, false);
        _blood.ChangeBloodReagent(target, "FerrochromicAcid");
        _blood.SpillAllSolutions(target);

        PlayMeatySound((EntityUid) uid, comp);

        _bodySystem.GibBody((EntityUid) uid);
    }

    #endregion

    #region Utilities

    public void OnAnatomicPanacea(EntityUid uid, ChangelingComponent comp, ref ActionAnatomicPanaceaEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Diphenhydramine", 5f),
            ("Arithrazine", 5f),
            ("Ethylredoxrazine", 5f)
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-panacea"), uid, uid);
        else return;
        PlayMeatySound(uid, comp);
    }
    public void OnAugmentedEyesight(EntityUid uid, ChangelingComponent comp, ref ActionAugmentedEyesightEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (HasComp<FlashImmunityComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-passive-active"), uid, uid);
            return;
        }

        EnsureComp<FlashImmunityComponent>(uid);
        _popup.PopupEntity(Loc.GetString("changeling-passive-activate"), uid, uid);
    }
    public void OnBiodegrade(EntityUid uid, ChangelingComponent comp, ref ActionBiodegradeEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (TryComp<CuffableComponent>(uid, out var cuffs) && cuffs.Container.ContainedEntities.Count > 0)
        {
            var cuff = cuffs.LastAddedCuffs;

            _cuffs.Uncuff(uid, cuffs.LastAddedCuffs, cuff);
            QueueDel(cuff);
        }

        var soln = new Solution();
        soln.AddReagent("PolytrinicAcid", 10f);

        if (_pull.IsPulled(uid))
        {
            var puller = Comp<PullableComponent>(uid).Puller;
            if (puller != null)
            {
                _puddle.TrySplashSpillAt((EntityUid) puller, Transform((EntityUid) puller).Coordinates, soln, out _);
                return;
            }
        }
        _puddle.TrySplashSpillAt(uid, Transform(uid).Coordinates, soln, out _);
    }
    public void OnChameleonSkin(EntityUid uid, ChangelingComponent comp, ref ActionChameleonSkinEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (HasComp<StealthComponent>(uid) && HasComp<StealthOnMoveComponent>(uid))
        {
            RemComp<StealthComponent>(uid);
            RemComp<StealthOnMoveComponent>(uid);
            _popup.PopupEntity(Loc.GetString("changeling-chameleon-end"), uid, uid);
            return;
        }

        EnsureComp<StealthComponent>(uid);
        _stealth.SetMinVisibility(uid, 0);

        var stealthOnMove = EnsureComp<StealthOnMoveComponent>(uid);
        stealthOnMove.MovementVisibilityRate = 1;

        _popup.PopupEntity(Loc.GetString("changeling-chameleon-start"), uid, uid);
    }
    public void OnEphedrineOverdose(EntityUid uid, ChangelingComponent comp, ref ActionEphedrineOverdoseEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        var stam = EnsureComp<StaminaComponent>(uid);
        stam.StaminaDamage = 0;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Desoxyephedrine", 5f)
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-inject"), uid, uid);
        else
        {
            _popup.PopupEntity(Loc.GetString("changeling-inject-fail"), uid, uid);
            return;
        }
    }
    // john space made me do this
    public void OnHealUltraSwag(EntityUid uid, ChangelingComponent comp, ref ActionFleshmendEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Ichor", 10f),
            ("TranexamicAcid", 5f)
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-fleshmend"), uid, uid);
        else return;
        PlayMeatySound(uid, comp);
    }
    public void OnLastResort(EntityUid uid, ChangelingComponent comp, ref ActionLastResortEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        comp.IsInLastResort = true;

        var newUid = TransformEntity(
            uid,
            protoId: "MobHeadcrab",
            comp: comp,
            dropInventory: true,
            transferDamage: false);

        if (newUid == null)
        {
            comp.IsInLastResort = false;
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            return;
        }

        _explosionSystem.QueueExplosion(
            (EntityUid)newUid,
            typeId: "Default",
            totalIntensity: 1,
            slope: 4,
            maxTileIntensity: 2);

        _actions.AddAction((EntityUid)newUid, "ActionLayEgg");

        PlayMeatySound((EntityUid)newUid, comp);
    }
    public void OnLesserForm(EntityUid uid, ChangelingComponent comp, ref ActionLesserFormEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        comp.IsInLesserForm = true;
        var newUid = TransformEntity(uid, protoId: "MobMonkey", comp: comp);
        if (newUid == null)
        {
            comp.IsInLesserForm = false;
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            return;
        }

        var targetUid = (EntityUid)newUid;

        var popupSelf = Loc.GetString("changeling-transform-lesser-self");
        var popupOthers = Loc.GetString("changeling-transform-lesser-others", ("user", Identity.Entity(uid, EntityManager)));

        _popup.PopupEntity(popupSelf, targetUid, targetUid);
        _popup.PopupEntity(popupOthers, targetUid, Filter.Pvs(targetUid).RemovePlayerByAttachedEntity(targetUid), true, PopupType.MediumCaution);

        PlayMeatySound((EntityUid)newUid, comp);
    }

    public void OnMindshieldFake(Entity<ChangelingComponent> ent, ref ActionMindshieldFakeEvent args)
    {
        if (!TryUseAbility(ent, ent.Comp, args))
            return;

        if (HasComp<MindShieldComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("changeling-mindshield-fail"), ent, ent, PopupType.Medium);
            return;
        }

        if (HasComp<FakeMindShieldComponent>(ent))
        {
            RemComp<FakeMindShieldComponent>(ent);
            _popup.PopupEntity(Loc.GetString("changeling-mindshield-end"), ent, ent);
            return;
        }

        EnsureComp<FakeMindShieldComponent>(ent);
        _popup.PopupEntity(Loc.GetString("changeling-mindshield-start"), ent, ent);
    }

    public void OnSpacesuit(EntityUid uid, ChangelingComponent comp, ref ActionSpacesuitEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleArmor(uid, comp, [(SpacesuitHelmetPrototype, "head"), (SpacesuitPrototype, "outerClothing")]))
        {
            _popup.PopupEntity(Loc.GetString("changeling-equip-armor-fail"), uid, uid);
            comp.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            return;
        }

        PlayMeatySound(uid, comp);
    }
    public void OnHivemindAccess(EntityUid uid, ChangelingComponent comp, ref ActionHivemindAccessEvent args)
    {
        if (!TryUseAbility(uid, comp, args))
            return;

        if (HasComp<HivemindComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("changeling-passive-active"), uid, uid);
            return;
        }

        EnsureComp<HivemindComponent>(uid);

        _popup.PopupEntity(Loc.GetString("changeling-hivemind-start"), uid, uid);
    }

    #endregion
}

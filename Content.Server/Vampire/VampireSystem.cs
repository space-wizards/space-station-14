using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Rotting;
using Content.Server.Bible.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Flash;
using Content.Server.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
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
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Frozen;
using System.Diagnostics;

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
    [Dependency] private readonly MetabolizerSystem _metabolism = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solution = default!;

    private FrozenDictionary<VampirePowerKey, VampirePowerPrototype> _cachedPowers = default!;
    private ReagentPrototype holyWaterReagent = default!;
    private ReagentPrototype bloodReagent = default!;
    private DamageSpecifier spaceDamage = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractHandEvent>(OnInteractWithHumanoid, before: new[] { typeof(InteractionPopupSystem) });
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodEvent>(DrinkDoAfter);

        SubscribeLocalEvent<VampireComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<VampireComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<VampireComponent, MobStateChangedEvent>(OnVampireStateChanged);
        SubscribeLocalEvent<VampireComponent, VampireUsePowerEvent>(OnUsePower);
        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(OnExamined);

        CachePowers();

        holyWaterReagent = _prototypeManager.Index<ReagentPrototype>("HolyWater");
        bloodReagent = _prototypeManager.Index<ReagentPrototype>("Blood");
        spaceDamage = new(_prototypeManager.Index<DamageTypePrototype>("Heat"), FixedPoint2.New(5));
    }

    /// <summary>
    /// Convert the entity into a vampire
    /// </summary>
    private void OnComponentInit(EntityUid uid, VampireComponent component, ComponentInit args)
    {
        //_solution.EnsureSolution(uid, component.BloodContainer);
        RemComp<BarotraumaComponent>(uid);
        RemComp<PerishableComponent>(uid);

        if (TryComp<TemperatureComponent>(uid, out var temperatureComponent))
            temperatureComponent.ColdDamageThreshold = 0;

        ConvertBody(uid);

        UpdateAbilities((uid, component));
    }

    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (component.FangsExtended && args.IsInDetailsRange)
            args.AddMarkup($"{Loc.GetString("vampire-fangs-extended-examine")}{Environment.NewLine}");
    }

    private void OnUseAreaPower(EntityUid uid, VampireComponent component, VampireUseAreaPowerEvent arg)
    {
        Entity<VampireComponent> vampire = (uid, component);

        if (!component.UnlockedPowers.ContainsKey(arg.Type))
            return;

        if (!_cachedPowers.TryGetValue(arg.Type, out var def))
            return;

        if (def.ActivationCost > 0 && def.ActivationCost > component.AvailableBlood)
        {
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), uid, uid, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        AddBlood(vampire, -def.ActivationCost);

        //Block if we are cuffed
        if (!def.UsableWhileCuffed && TryComp<CuffableComponent>(uid, out var cuffable) && !cuffable.CanStillInteract)
            return;

        //Block if we are stunned
        if (!def.UsableWhileStunned && HasComp<StunnedComponent>(uid))
            return;

        //Block if we are muzzled
        if (!def.UsableWhileMuffled && TryComp<ReplacementAccentComponent>(uid, out var accent) && accent.Accent.Equals("mumble"))
            return;

        if (def.ActivationEffect != null)
            Spawn(def.ActivationEffect, _transform.GetMapCoordinates(Transform(vampire.Owner)));

        if (def.ActivationSound != null)
            _audio.PlayPvs(def.ActivationSound, uid);

        _action.StartUseDelay(component.UnlockedPowers[arg.Type]);

        //TODO: Rewrite when a magic system is introduced
        switch (arg.Type)
        {
            case VampirePowerKey.ToggleFangs:
                {
                    ToggleFangs(vampire);
                    break;
                }
            case VampirePowerKey.DeathsEmbrace:
                {
                    TryMoveToCoffin(vampire);
                    break;
                }
            case VampirePowerKey.Glare:
                {
                    Glare(vampire);
                    break;
                }
            case VampirePowerKey.Screech:
                {
                    Screech(vampire);
                    break;
                }
            default:
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query1 = EntityQueryEnumerator<VampireHealingComponent>();
        while (query1.MoveNext(out var uid, out var vampireHealingComponent))
        {
            if (vampireHealingComponent.NextHealTick <= 0)
            {
                vampireHealingComponent.NextHealTick = vampireHealingComponent.HealTickInterval.TotalSeconds;
                DoCoffinHeal(uid, vampireHealingComponent);
            }
            vampireHealingComponent.NextHealTick -= frameTime;
        }

        var query2 = EntityQueryEnumerator<VampireComponent>();
        while (query2.MoveNext(out var uid, out var vampireComponent))
        {
            if (IsInSpace(uid))
            {
                if (vampireComponent.NextSpaceDamageTick <= 0)
                {
                    vampireComponent.NextSpaceDamageTick = vampireComponent.SpaceDamageInterval.TotalSeconds;
                    DoSpaceDamage(uid, vampireComponent);
                }
                vampireComponent.NextSpaceDamageTick -= frameTime;
            }
        }
    }

    private void UpdateAbilities(Entity<VampireComponent> vampire)
    {
        foreach (var power in _cachedPowers.Values)
        {
            if (power.UnlockRequirement <= vampire.Comp.AvailableBlood)
                UnlockAbility(vampire, power.Key);
        }
    }
    private void UnlockAbility(Entity<VampireComponent> vampire, VampirePowerKey power)
    {
        if (vampire.Comp.UnlockedPowers.ContainsKey(power))
            return;

        if (_cachedPowers.TryGetValue(power, out var def) && def.ActionPrototype != null)
        {
            var actionUid = _action.AddAction(vampire.Owner, def.ActionPrototype);
            if (!actionUid.HasValue)
                return;
            vampire.Comp.UnlockedPowers.Add(power, actionUid.Value);
        }
        //Play unlock sound
        //Show popup
    }

    private void Screech(Entity<VampireComponent> vampire)
    {
        var transform = Transform(vampire.Owner);

        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, 3, LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (HasComp<BibleUserComponent>(entity))
                return;
        }
    }
    private void Glare(Entity<VampireComponent> vampire)
    {
        var transform = Transform(vampire.Owner);

        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, 2, LookupFlags.Dynamic))
        {
            if (HasComp<FlashableComponent>(entity))
                return;

            if (HasComp<BibleUserComponent>(entity))
                return;

            _flash.Flash(entity, null, null, 10, 0.8f);
        }
    }

    #region Deaths Embrace
    private void OnVampireStateChanged(EntityUid uid, VampireComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            OnUsePower(uid, component, new() { Type = VampirePowerKey.DeathsEmbrace });
    }
    private void OnInsertedIntoContainer(EntityUid uid, VampireComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<CoffinComponent>(args.Container.Owner, out var coffinComp))
        {
            component.HomeCoffin = args.Container.Owner;
            var comp = new VampireHealingComponent { Damage = coffinComp.Damage };
            AddComp(args.Entity, comp);
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

        _entityStorage.CloseStorage(vampire.Comp.HomeCoffin.Value, coffinEntityStorage);

        return _entityStorage.Insert(vampire, vampire.Comp.HomeCoffin.Value, coffinEntityStorage);
    }
    private void DoCoffinHeal(EntityUid vampireUid, VampireHealingComponent healingComponent)
    {
        if (!_container.TryGetOuterContainer(vampireUid, Transform(vampireUid), out var container))
            return;

        if (!HasComp<CoffinComponent>(container.Owner))
            return;

        _damageableSystem.TryChangeDamage(vampireUid, healingComponent.Damage, true, origin: container.Owner);

        if (!TryComp<MobStateComponent>(vampireUid, out var mobStateComponent))
            return;

        if (!_mobState.IsDead(vampireUid, mobStateComponent))
            return;

        if (!_mobThreshold.TryGetThresholdForState(vampireUid, MobState.Dead, out var threshold))
            return;

        if (!TryComp<DamageableComponent>(vampireUid, out var damageableComponent))
            return;

        if (damageableComponent.TotalDamage < threshold * 0.75)
        {
            _mobState.ChangeMobState(vampireUid, MobState.Critical, mobStateComponent, container.Owner);
        }
    }
    #endregion

    #region Blood Drinking
    private void ToggleFangs(Entity<VampireComponent> vampire)
    {
        vampire.Comp.FangsExtended = !vampire.Comp.FangsExtended;
        var popupText = Loc.GetString(vampire.Comp.FangsExtended ? "vampire-fangs-extended" : "vampire-fangs-retracted");
        _popup.PopupEntity(popupText, vampire.Owner, vampire.Owner);
    }
    private void OnInteractWithHumanoid(EntityUid uid, HumanoidAppearanceComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;

        args.Handled = TryDrink(args.Target, args.User);
    }
    private bool TryDrink(EntityUid target, EntityUid drinker, VampireComponent? vampireComponent = null)
    {
        if (!Resolve(drinker, ref vampireComponent, false))
            return false;

        //Do a precheck
        if (!vampireComponent.FangsExtended)
            return false;

        if (!_interaction.InRangeUnobstructed(drinker, target, popup: true))
            return false;

        if (_food.IsMouthBlocked(target, drinker))
            return false;

        if (_rotting.IsRotten(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), drinker, drinker, Shared.Popups.PopupType.SmallCaution);
            return false;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, drinker, vampireComponent.BloodDrainDelay,
        new VampireDrinkBloodEvent(),
        eventTarget: drinker,
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

        if (_food.IsMouthBlocked(args.Target.Value, entity))
            return;

        if (!entity.Comp.FangsExtended)
            return;

        if (_rotting.IsRotten(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), args.User, args.User, Shared.Popups.PopupType.SmallCaution);
            return;
        }

        if (!TryComp<BloodstreamComponent>(args.Target, out var targetBloodstream) || targetBloodstream == null || targetBloodstream.BloodSolution == null)
            return;

        if (!_cachedPowers.TryGetValue(VampirePowerKey.DrinkBlood, out var def) || def == null)
            return;

        //Ensure there is enough blood to drain
        var victimBloodRemaining = targetBloodstream.BloodSolution.Value.Comp.Solution.Volume;
        if (victimBloodRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), entity.Owner, entity.Owner, Shared.Popups.PopupType.SmallCaution);
            return;
        }
        var volumeToConsume = Math.Min((float)victimBloodRemaining.Value, def.ActivationCost);

        if (!IngestBlood(entity, volumeToConsume))
            return;

        if (!_blood.TryModifyBloodLevel(args.Target.Value, -volumeToConsume))
            return;

        _audio.PlayPvs(def.ActivationSound, entity.Owner, AudioParams.Default.WithVolume(-3f));

        if (HasComp<BibleUserComponent>(args.Target.Value))
        {
            //Scream, do burn damage
            //Thou shall not feed upon the blood of the holy
            InjectHolyWater(entity, 5);
            return;
        }
        else
        {
            AddBlood(entity, volumeToConsume);
        }

        //Update abilities, add new unlocks
        UpdateAbilities(entity);

        args.Repeat = true;
    }

    private void InjectHolyWater(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        var solution = new ReagentQuantity(holyWaterReagent.ID, quantity, null);
        if (TryComp<BloodstreamComponent>(vampire.Owner, out var bloodStream) && bloodStream.ChemicalSolution != null)
        {
            _solution.TryAddReagent(bloodStream.ChemicalSolution.Value, solution, out _);
        }
    }
    private bool IngestBlood(Entity<VampireComponent> vampire, float quantity)
    {
        if (TryComp<BodyComponent>(vampire.Owner, out var body) && _body.TryGetBodyOrganComponents<StomachComponent>(vampire.Owner, out var stomachs, body))
        {
            var ingestedSolution = new Solution(bloodReagent.ID, quantity);
            var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, ingestedSolution, stomach.Comp));
            if (firstStomach == null)
            {
                //We are full
                _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), vampire.Owner, vampire.Owner, Shared.Popups.PopupType.SmallCaution);
                return false;
            }
            _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, ingestedSolution, firstStomach.Value.Comp);
            return true;
        }

        //No stomach
        return false;
    }
    private void AddBlood(Entity<VampireComponent> vampire, float quantity)
    {
        vampire.Comp.TotalBloodDrank += quantity;
        vampire.Comp.AvailableBlood += quantity;
    }
    #endregion

    private void DoSpaceDamage(EntityUid vampireUid, VampireComponent component)
    {
        _damageableSystem.TryChangeDamage(vampireUid, spaceDamage, true, origin: vampireUid);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), vampireUid, vampireUid, Shared.Popups.PopupType.LargeCaution);
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

    /// <summary>
    /// Convert the players body into a vampire
    /// Alternative to this would be creating a dedicated vampire race
    /// But i want the player to look 'normal' and keep the same customisations as the non vampire player
    /// </summary>
    /// <param name="vampire">Which entity to convert</param>
    private void ConvertBody(EntityUid vampire)
    {
        var metabolizerTypes = new HashSet<string>() { "bloodsucker", "vampire" }; //Heal from drinking blood, and be damaged by drinking holy water
        var specialDigestion = new EntityWhitelist() { Tags = new() { "Pill" } }; //Restrict Diet

        if (!TryComp<BodyComponent>(vampire, out var bodyComponent))
            return;

        //Add vampire and bloodsucker to all metabolizing organs
        //And restrict diet to Pills (and liquids)
        foreach(var organ in _body.GetBodyOrgans(vampire, bodyComponent))
        {
            if (TryComp<MetabolizerComponent>(organ.Id, out var metabolizer))
            {
                if (TryComp<StomachComponent>(organ.Id, out var stomachComponent))
                {
                    //Override the stomach, prevents humans getting sick when ingesting blood
                    _metabolism.SetMetabolizerTypes(metabolizer, metabolizerTypes);
                    _stomach.SetSpecialDigestible(stomachComponent, specialDigestion);
                }
                else
                {
                    //Otherwise just add the metabolizers on
                    var tempMetabolizer = metabolizer.MetabolizerTypes ?? new HashSet<string>();
                    foreach (var t in metabolizerTypes)
                        tempMetabolizer.Add(t);

                    _metabolism.SetMetabolizerTypes(metabolizer, tempMetabolizer);
                }
            }
        }

        //Take damage from holy water splash
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                reactive.ReactiveGroups = new();

            reactive.ReactiveGroups.Add("Unholy", new() { ReactionMethod.Touch });
        }

        DamageSpecifier meleeDamage = new(_prototypeManager.Index<DamageTypePrototype>("Slash"), FixedPoint2.New(10));

        //Extra melee power
        if (TryComp<MeleeWeaponComponent>(vampire, out var melee))
        {
            melee.Damage = meleeDamage;
            melee.Animation = "WeaponArcSlash";
            melee.HitSound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");
        }
    }
    //Remove weakeness to holy items
    private void MakeImmuneToHoly(EntityUid vampire)
    {
        if (!TryComp<BodyComponent>(vampire, out var bodyComponent))
            return;

        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                return;

            reactive.ReactiveGroups.Remove("Unholy");
        }
    }
    private void CachePowers()
    {
        var tempDict = new Dictionary<VampirePowerKey, VampirePowerPrototype>();
        foreach (var power in _prototypeManager.EnumeratePrototypes<VampirePowerPrototype>())
        {
            tempDict.Add(power.Key, power);
        }

        _cachedPowers = tempDict.ToFrozenDictionary();
    }
}

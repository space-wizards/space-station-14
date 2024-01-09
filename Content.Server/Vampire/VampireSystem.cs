using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Rotting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solution = default!;
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
    }

    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (component.FangsExtended && args.IsInDetailsRange)
            args.AddMarkup($"{Environment.NewLine}{Loc.GetString("vampire-fangs-extended-examine")}", 3);
    }


    private void OnUsePower(EntityUid uid, VampireComponent component, VampireUsePowerEvent arg)
    {
        Entity<VampireComponent> vampire = (uid, component);

        if (!component.VampirePowers.TryGetValue(arg.Type, out var def))
            return;

        if (def.ActivationCost > 0)
        {
            if (GetBlood(vampire) < def.ActivationCost)
            {
                _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), uid, uid, Shared.Popups.PopupType.MediumCaution);
                return;
            }

            AddBlood(vampire, -def.ActivationCost);
        }

        if (def.ActivationEffect != null)
            Spawn(def.ActivationEffect, _transform.GetMapCoordinates(Transform(vampire.Owner)));

        if (def.ActivationSound != null)
            _audio.PlayPvs(def.ActivationSound, uid);

        switch (arg.Type)
        {
            case VampirePower.ToggleFangs:
                {
                    ToggleFangs(vampire);
                    break;
                }
            case VampirePower.DeathsEmbrace:
                {
                    TryMoveToCoffin(vampire);
                    break;
                }

            default:
                break;
        }
    }

    /// <summary>
    /// Ensure the player has a blood storage container, is immune to pressure, o2 and low temperatures
    /// </summary>
    private void OnComponentInit(EntityUid uid, VampireComponent component, ComponentInit args)
    {
        _solution.EnsureSolution(uid, component.BloodContainer);
        RemComp<BarotraumaComponent>(uid);
        RemComp<PerishableComponent>(uid);

        if (TryComp<TemperatureComponent>(uid, out var temperatureComponent))
            temperatureComponent.ColdDamageThreshold = 0;

        component.SpaceDamage = new(_prototypeManager.Index<DamageGroupPrototype>("Burn"), FixedPoint2.New(5));

        UpdateAbilities((uid, component));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator< VampireComponent>();
        while (query.MoveNext(out var vampireUid, out var vampireComponent))
        {
            if (TryComp<VampireHealingComponent>(vampireUid, out var vampireHealingComponent))
            {
                if (vampireHealingComponent.NextHealTick > 0)
                {
                    vampireHealingComponent.NextHealTick -= frameTime;
                    continue;
                }

                vampireHealingComponent.NextHealTick = vampireHealingComponent.HealTickInterval.TotalSeconds;
                DoCoffinHeal(vampireUid, vampireHealingComponent);
            }

            if (IsInSpace(vampireUid))
            {
                if (vampireComponent.NextSpaceDamageTick > 0)
                {
                    vampireComponent.NextSpaceDamageTick -= frameTime;
                }
                else
                {
                    vampireComponent.NextSpaceDamageTick = vampireComponent.SpaceDamageInterval.TotalSeconds;
                    DoSpaceDamage(vampireUid, vampireComponent);
                }
            }
        }
    }

    private void UpdateAbilities(Entity<VampireComponent> vampire)
    {
        var availableBlood = GetBlood(vampire);
        foreach (var power in vampire.Comp.VampirePowers)
        {
            var def = power.Value;
            if (vampire.Comp.UnlockedPowers.Contains(power.Key))
                continue;

            if (def.UnlockCost <= availableBlood)
                UnlockAbility(vampire, power.Key);
        }
    }
    private void UnlockAbility(Entity<VampireComponent> vampire, VampirePower power)
    {
        vampire.Comp.UnlockedPowers.Add(power);
        if (vampire.Comp.VampirePowers.TryGetValue(power, out var def) && def.ActionPrototype != null)
        {
            _action.AddAction(vampire.Owner, ref def.Action, def.ActionPrototype, vampire.Owner);
        }
        //Play unlock sound
        //Show popup
    }

    #region Deaths Embrace
    private void OnVampireStateChanged(EntityUid uid, VampireComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            OnUsePower(uid, component, new() { Type = VampirePower.DeathsEmbrace });
    }
    private void OnInsertedIntoContainer(EntityUid uid, VampireComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<CoffinComponent>(args.Container.Owner, out var coffinComp))
        {
            component.HomeCoffin = args.Container.Owner;
            var comp = new VampireHealingComponent { Damage = coffinComp.Damage };
            AddComp(args.Entity, comp);
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
    #endregion

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

        if (_rot)
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

        if (!TryComp<BloodstreamComponent>(args.Target, out var targetBloodstream))
            return;

        if (targetBloodstream.BloodSolution == null)
            return;

        if (!entity.Comp.VampirePowers.TryGetValue(VampirePower.DrinkBlood, out var def))
            return;

        //Ensure there is enough blood to drain
        if (targetBloodstream.BloodSolution?.Comp.Solution.Volume < def.ActivationCost)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), entity.Owner, entity.Owner, Shared.Popups.PopupType.SmallCaution);
            return;
        }

        if (!_blood.TryModifyBloodLevel(args.Target.Value, def.ActivationCost))
            return;

        AddBlood(entity, -def.ActivationCost);

        _audio.PlayPvs(def.ActivationSound, entity.Owner, AudioParams.Default.WithVolume(-3f));

        //Update abilities, add new unlocks
        UpdateAbilities(entity);

        args.Repeat = true;
    }
    #endregion

    private void DoSpaceDamage(EntityUid vampireUid, VampireComponent component)
    {
        _damageableSystem.TryChangeDamage(vampireUid, component.SpaceDamage, true, origin: vampireUid);
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

    private FixedPoint2? GetBlood(Entity<VampireComponent> vampire)
    {
        if (!_solution.TryGetSolution(vampire.Owner, vampire.Comp.BloodContainer, out var vampireBloodContainer))
            return null;

        return vampireBloodContainer.Value.Comp.Solution.Volume;
    }
    private void AddBlood(Entity<VampireComponent> vampire, Solution solution) {
        AddBlood(vampire, solution.Volume);
    }
    private void AddBlood(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        if (!_solution.TryGetSolution(vampire.Owner, vampire.Comp.BloodContainer, out var vampireBloodContainer))
            return;

        //Convert everything into 'blood'
        //No point restricting people to only feeding on people with their blood type
        var solution = new Solution("blood", quantity);
        _solution.TryAddSolution(vampireBloodContainer.Value, solution);
    }
}

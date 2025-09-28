using Content.Server.Administration.Logs;
using Content.Server.Atmos.Rotting;
using Content.Server.Beam;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Mind;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Prayer;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Server.Charges;
using Content.Shared.Charges.Components;
using Content.Shared.Actions.Components;
using Content.Server.Administration.Systems;
using Content.Shared.Maps;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MetabolizerSystem _metabolism = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedVampireSystem _vampire = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ChargesSystem _charges = default!;
    [Dependency] private readonly StarlightEntitySystem _entities = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<VampireComponent, VampireSelfPowerEvent>(OnUseSelfPower);
        //SubscribeLocalEvent<VampireComponent, VampireTargetedPowerEvent>(OnUseTargetedPower);
        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<VampireComponent, VampireBloodChangedEvent>(OnVampireBloodChangedEvent);

        SubscribeLocalEvent<VampireComponent, AfterAutoHandleStateEvent>(GetState);
        SubscribeLocalEvent<VampireComponent, VampireMutationPrototypeSelectedMessage>(OnMutationSelected);

        InitializePowers();
        InitializeObjectives();
    }

    /// <summary>
    /// Handles healing, stealth and damaging in space
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var stealthQuery = EntityQueryEnumerator<VampireComponent, VampireSealthComponent>();
        while (stealthQuery.MoveNext(out var uid, out var vampire, out var stealth))
        {
            if (vampire == null || stealth == null)
                continue;

            if (stealth.NextStealthTick <= 0)
            {
                stealth.NextStealthTick = 1;
                if (!SubtractBloodEssence((uid, vampire), stealth.Upkeep) || _vampire.GetBloodEssence(uid) < FixedPoint2.New(300))
                {
                    RemCompDeferred<StealthOnMoveComponent>(uid);
                    RemCompDeferred<StealthComponent>(uid);
                    RemCompDeferred<VampireSealthComponent>(uid);
                    _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), uid, uid);
                    if (_vampire.GetBloodEssence(uid) < FixedPoint2.New(300))
                    {
                        var vampireUid = new Entity<VampireComponent>(uid, vampire);
                        var ev = new VampireBloodChangedEvent();
                        RaiseLocalEvent(vampireUid, ev);
                    }
                }
            }
            stealth.NextStealthTick -= frameTime;
        }

        var healingQuery = EntityQueryEnumerator<VampireComponent, VampireHealingComponent>();
        while (healingQuery.MoveNext(out var uid, out _, out var healing))
        {
            if (healing == null)
                continue;

            if (healing.NextHealTick <= 0)
            {
                healing.NextHealTick = 1;
                DoCoffinHeal(uid, healing);
            }
            healing.NextHealTick -= frameTime;
        }

        var spaceQuery = EntityQueryEnumerator<VampireComponent, VampireSpaceDamageComponent, DamageableComponent>();
        while (spaceQuery.MoveNext(out var uid, out var vampire, out var spacedamage, out var damage))
        {
            if (vampire == null || spacedamage == null)
                continue;

            if (IsInSpace(uid))
            {
                if (spacedamage.NextSpaceDamageTick <= 0)
                {
                    spacedamage.NextSpaceDamageTick = 1;
                    if (!SubtractBloodEssence((uid, vampire), FixedPoint2.New(1)))
                        DoSpaceDamage(uid, vampire, damage);
                }
                spacedamage.NextSpaceDamageTick -= frameTime;
            }
        }

        var strengthQuery = EntityQueryEnumerator<VampireComponent, VampireStrengthComponent>();
        while (strengthQuery.MoveNext(out var uid, out var vampire, out var strength))
        {
            if (vampire == null || strength == null)
                continue;

            if (strength.NextTick <= 0)
            {
                strength.NextTick = 1;
                FixedPoint2 bloodNeed = default;
                if (strength.Power == "UnholyStrength")
                    bloodNeed = FixedPoint2.New(200);
                else
                    bloodNeed = FixedPoint2.New(300);
                if (!SubtractBloodEssence((uid, vampire), strength.Upkeep) || _vampire.GetBloodEssence(uid) < bloodNeed)
                {
                    var vampireUid = new Entity<VampireComponent>(uid, vampire);
                    UnnaturalStrength(vampireUid);
                    _popup.PopupEntity(Loc.GetString("vampire-cloak-disable"), uid, uid);
                }
            }
            strength.NextTick -= frameTime;
        }
    }

    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (HasComp<VampireFangsExtendedComponent>(uid) && args.IsInDetailsRange && _ingestion.HasMouthAvailable(args.Examiner, uid))
            args.AddMarkup($"{Loc.GetString("vampire-fangs-extended-examine")}{Environment.NewLine}");
    }
    private bool AddBloodEssence(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        if (quantity < 0)
            return false;

        vampire.Comp.TotalBloodDrank += quantity.Float();
        vampire.Comp.Balance[VampireComponent.CurrencyProto] += quantity;

        UpdateBloodDisplay(vampire);

        var ev = new VampireBloodChangedEvent();
        RaiseLocalEvent(vampire, ev);

        return true;
    }
    private bool SubtractBloodEssence(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        if (quantity < 0)
            return false;

        if (vampire.Comp.Balance[VampireComponent.CurrencyProto] < quantity)
            return false;

        vampire.Comp.Balance[VampireComponent.CurrencyProto] -= quantity;

        UpdateBloodDisplay(vampire);

        var ev = new VampireBloodChangedEvent();
        RaiseLocalEvent(vampire, ev);

        return true;
    }
    /// <summary>
    /// Use the charges display on SummonHeirloom to show the remaining blood essence
    /// </summary>
    /// <param name="vampire"></param>
    public void UpdateBloodDisplay(EntityUid vampire)
    {
        if (!TryComp<VampireComponent>(vampire, out var comp))
            return;

        //Sanity check, you never know who is going to touch this code
        if (!comp.Balance.TryGetValue(VampireComponent.CurrencyProto, out var balance))
            return;

        var chargeDisplay = (int) Math.Round((decimal) balance);
        var mutationsAction = GetPowerEntity(comp, VampireComponent.MutationsActionPrototype);

        if (mutationsAction == null)
            return;

        if(TryComp<LimitedChargesComponent>(mutationsAction, out var charges))
            _charges.SetCharges((mutationsAction.Value, charges), chargeDisplay);
    }

    private void OnVampireBloodChangedEvent(EntityUid uid, VampireComponent component, VampireBloodChangedEvent args)
    {
        var bloodEssence = _vampire.GetBloodEssence(uid);

        AbilityInfo entity = default;

        if (TryComp<VampireAlertComponent>(uid, out var alertComp))
            _vampire.SetAlertBloodAmount(alertComp,_vampire.GetBloodEssence(uid).Int());

        if (component.actionEntities.TryGetValue("ActionVampireCloakOfDarkness", out entity) && !HasComp<VampireSealthComponent>(uid) && _vampire.GetBloodEssence(uid) < FixedPoint2.New(300))
            component.actionEntities.Remove("ActionVampireCloakOfDarkness");

        UpdateAbilities(uid, component , VampireComponent.MutationsActionPrototype, "MutationsMenu" , bloodEssence >= FixedPoint2.New(50) && !HasComp<VampireSealthComponent>(uid));

        //Hemomancer

        // Blood Steal
        UpdateAbilities(uid, component , "ActionVampireBloodSteal", "BloodSteal" , bloodEssence >= FixedPoint2.New(200) && component.CurrentMutation == VampireMutationsType.Hemomancer);

        // Screech
        UpdateAbilities(uid, component , "ActionVampireScreech", "Screech" , bloodEssence >= FixedPoint2.New(300) && component.CurrentMutation == VampireMutationsType.Hemomancer);

        //Umbrae

        //Glare
        UpdateAbilities(uid, component , "ActionVampireGlare", "Glare" , bloodEssence >= FixedPoint2.New(200) && component.CurrentMutation == VampireMutationsType.Umbrae);

        //CloakOfDarkness
        UpdateAbilities(uid, component , "ActionVampireCloakOfDarkness", "CloakOfDarkness" , bloodEssence >= FixedPoint2.New(300) && component.CurrentMutation == VampireMutationsType.Umbrae);


        //Gargantua

        UpdateAbilities(uid, component , "ActionVampireUnholyStrength", "UnholyStrength" , bloodEssence >= FixedPoint2.New(200) && component.CurrentMutation == VampireMutationsType.Gargantua);

        UpdateAbilities(uid, component , "ActionVampireSupernaturalStrength", "SupernaturalStrength" , bloodEssence >= FixedPoint2.New(300) && component.CurrentMutation == VampireMutationsType.Gargantua);

        //Bestia

        UpdateAbilities(uid, component , "ActionVampireBatform", "PolymorphBat" , bloodEssence >= FixedPoint2.New(200) && component.CurrentMutation == VampireMutationsType.Bestia);

        UpdateAbilities(uid, component , "ActionVampireMouseform", "PolymorphMouse" , bloodEssence >= FixedPoint2.New(300) && component.CurrentMutation == VampireMutationsType.Bestia);
    }

    private void UpdateAbilities(EntityUid uid, VampireComponent component, string actionId, string? powerId, bool addAction)
    {
        EntityUid? actionEntity = null;
        if (addAction)
        {
            if (!component.actionEntities.ContainsKey(actionId))
            {
                _action.AddAction(uid, ref actionEntity, actionId);
                if (actionEntity != null)
                {
                    component.actionEntities[actionId] = new AbilityInfo(_entityManager.GetNetEntity(uid), _entityManager.GetNetEntity(actionEntity.Value));
                    if (powerId != null && !component.UnlockedPowers.ContainsKey(powerId))
                        component.UnlockedPowers.Add(powerId, GetNetEntity(actionEntity.Value));
                }
            }
        }
        else
        {
            if (component.actionEntities.TryGetValue(actionId, out var abilityInfo) && _entityManager.GetEntity(abilityInfo.Owner) == uid)
            {
                if (TryComp(uid, out ActionsComponent? comp))
                {
                    var action = _entities.Entity<ActionComponent>(_entityManager.GetEntity(abilityInfo.Action));
                    _action.RemoveAction(action);
                    _actionContainer.RemoveAction(action.Owner);
                    component.actionEntities.Remove(actionId);
                    if (powerId != null && component.UnlockedPowers.ContainsKey(powerId))
                        component.UnlockedPowers.Remove(powerId);
                }
            }
        }
        Dirty(uid, component);
    }

    private void DoSpaceDamage(EntityUid uid, VampireComponent comp, DamageableComponent damage)
    {
        var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Heat"), 2.5);
        _damageableSystem.TryChangeDamage(uid, damageSpec, true, false, damage, uid);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), uid, uid, PopupType.LargeCaution);
    }
    private bool IsInSpace(EntityUid vampireUid)
    {
        var vampireTransform = Transform(vampireUid);
        var vampirePosition = _transform.GetMapCoordinates(vampireTransform);

        if (!_mapMan.TryFindGridAt(vampirePosition, out _, out var grid))
            return true;

        if (!_mapSystem.TryGetTileRef(vampireUid, grid, vampireTransform.Coordinates, out var tileRef))
            return true;

        return tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef) || _turf.GetContentTileDefinition(tileRef).ID == "Lattice";
    }

    private bool IsNearPrayable(EntityUid vampireUid)
    {
        var mapCoords = _transform.GetMapCoordinates(vampireUid);

        var nearbyPrayables = _entityLookup.GetEntitiesInRange<PrayableComponent>(mapCoords, 5);
        foreach (var prayable in nearbyPrayables)
        {
            if (Transform(prayable).Anchored)
                return true;
        }

        return false;
    }

    private void OnMutationSelected(EntityUid uid, VampireComponent component, VampireMutationPrototypeSelectedMessage args)
    {
        if (component.CurrentMutation == args.SelectedId)
            return;
        ChangeMutation(uid, args.SelectedId, component);
    }
    private void ChangeMutation(EntityUid uid, VampireMutationsType newMutation, VampireComponent component)
    {
        var vampire = new Entity<VampireComponent>(uid, component);
        if (SubtractBloodEssence(vampire, FixedPoint2.New(50)))
        {
            component.CurrentMutation = newMutation;
            UpdateUi(uid, component);
            var ev = new VampireBloodChangedEvent();
            RaiseLocalEvent(uid, ev);
            TryOpenUi(uid, component.Owner, component);
        }
    }

    private void GetState(EntityUid uid, VampireComponent component, ref AfterAutoHandleStateEvent args) => args.State = new VampireMutationComponentState
    {
        SelectedMutation = component.CurrentMutation
    };

    private void TryOpenUi(EntityUid uid, EntityUid user, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(user, out ActorComponent? actor))
            return;
        _uiSystem.TryToggleUi(uid, VampireMutationUiKey.Key, actor.PlayerSession);
    }

    public void UpdateUi(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        var state = new VampireMutationBoundUserInterfaceState(component.VampireMutations, component.CurrentMutation);
        _uiSystem.SetUiState(uid, VampireMutationUiKey.Key, state);
    }
}

using Content.Server.Atmos.Rotting;
using Content.Server.Beam;
using Content.Server.Bed.Sleep;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Speech.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Components;
using Content.Server.Store.Events;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : EntitySystem
{
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
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractHandEvent>(OnInteractWithHumanoid, before: new[] { typeof(InteractionPopupSystem), typeof(SleepingSystem) });

        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodEvent>(DrinkDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseEvent>(HypnotiseDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireSummonHeirloomEvent>(OnSummonHeirloom);
        SubscribeLocalEvent<VampireComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<VampireComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<VampireComponent, MobStateChangedEvent>(OnVampireStateChanged);
        SubscribeLocalEvent<VampireComponent, VampireSelfPowerEvent>(OnUseSelfPower);
        SubscribeLocalEvent<VampireComponent, VampireTargetedPowerEvent>(OnUseTargetedPower);
        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<VampireHeirloomComponent, UseInHandEvent>(OnUseHeirloom);
        SubscribeLocalEvent<VampireHeirloomComponent, StorePurchasedActionEvent>(OnStorePurchase);
    }

    /// <summary>
    /// Handles healing and damaging in space
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var stealthQuery = EntityQueryEnumerator<VampireComponent, VampireSealthComponent>();
        while (stealthQuery.MoveNext(out var uid, out var vampire, out var stealth))
        {
            if (stealth.NextStealthTick <= 0)
            {
                stealth.NextStealthTick = 1;
                if (!AddBloodEssence((uid, vampire), -stealth.Upkeep))
                    RemCompDeferred<VampireSealthComponent>(uid);
            }
            stealth.NextStealthTick -= frameTime;
        }

        var healingQuery = EntityQueryEnumerator<VampireComponent, VampireHealingComponent>();
        while (healingQuery.MoveNext(out var uid, out var vampire, out var healing))
        {
            if (healing.NextHealTick <= 0)
            {
                healing.NextHealTick = 1;
                DoCoffinHeal(uid, healing);
            }
            healing.NextHealTick -= frameTime;
        }

        /*var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var vampireComponent))
        {
            var vampire = (uid, vampireComponent);

            if (IsInSpace(uid))
            {
                if (vampireComponent.NextSpaceDamageTick <= 0)
                {
                    vampireComponent.NextSpaceDamageTick = 1;
                    DoSpaceDamage(vampire);
                }
                vampireComponent.NextSpaceDamageTick -= frameTime;
            }
        }*/
    }
    private void OnComponentStartup(EntityUid uid, VampireComponent component, ComponentStartup args)
    {
        MakeVampire(uid);
    }

    private void OnUseHeirloom(EntityUid uid, VampireHeirloomComponent component, UseInHandEvent args)
    {
        //Ensure the user is a vampire
        if (!HasComp<VampireComponent>(args.User))
            return;

        //Only allow the heirloom owner to use this - prevent stealing others blood essence
        //TODO: Popup
        if (component.Owner != args.User)
            return;

        //And open the UI
        _store.ToggleUi(args.User, uid);
    }

    private void OnStorePurchase(EntityUid uid, VampireHeirloomComponent component, ref StorePurchasedActionEvent ev)
    => OnStorePurchase(ev.Purchaser, ev.Action);
    private void OnStorePurchase(EntityUid purchaser, EntityUid purchasedAction)
    {
        if (!TryComp<VampireComponent>(purchaser, out var vampireComponent))
            return;

        if (TryComp<InstantActionComponent>(purchasedAction, out var instantAction) && instantAction.Event != null && instantAction.Event is VampireSelfPowerEvent)
        {
            var vampirePower = instantAction.Event as VampireSelfPowerEvent;
            vampireComponent.UnlockedPowers[vampirePower!.Type] = purchasedAction;
            return;
        }
        if (TryComp<EntityTargetActionComponent>(purchasedAction, out var targetAction) && targetAction.Event != null && targetAction.Event is VampireTargetedPowerEvent)
        {
            var vampirePower = targetAction.Event as VampireTargetedPowerEvent;
            vampireComponent.UnlockedPowers[vampirePower!.Type] = purchasedAction;
            return;
        }
    }


    /// <summary>
    /// Called by the store when a new passive ability is purchased
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    /// <exception cref="NotImplementedException"></exception>
    /*private void OnUnlockPassive(EntityUid uid, VampireComponent component, VampireUnlockPassiveEvent args)
    {
        throw new NotImplementedException();
    }*/


    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (IsAbilityActive(component, VampirePowerKey.ToggleFangs) && args.IsInDetailsRange && !_food.IsMouthBlocked(uid))
            args.AddMarkup($"{Loc.GetString("vampire-fangs-extended-examine")}{Environment.NewLine}");
    }
    private bool AddBloodEssence(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        vampire.Comp.TotalBloodDrank += quantity.Float();
        vampire.Comp.Balance[VampireComponent.CurrencyProto] += quantity;
        return true;
    }
    private FixedPoint2 GetBloodEssence(Entity<VampireComponent> vampire)
    {
        if (!TryComp<StoreComponent>(vampire, out var storeComp))
            return 0;
        var currencies = storeComp.Balance;
        if (!currencies.TryGetValue(VampireComponent.CurrencyProto, out var val))
            return 0;
        return val;
    }

    private bool IsAbilityUnlocked(VampireComponent vampire, VampirePowerKey ability)
    {
        return vampire.UnlockedPowers.ContainsKey(ability);
    }
    private bool IsAbilityActive(VampireComponent vampire, VampirePowerKey ability)
    {
        return vampire.AbilityStates.Contains(ability);
    }
    private bool SetAbilityActive(VampireComponent vampire, VampirePowerKey ability, bool active)
    {
        if (active)
        {
            return vampire.AbilityStates.Add(ability);
        }
        else
        {
            return vampire.AbilityStates.Remove(ability);
        }
    }
    private EntityUid? GetAbilityEntity(VampireComponent vampire, VampirePowerKey ability)
    {
        if (IsAbilityUnlocked(vampire, ability))
            return vampire.UnlockedPowers[ability];
        return null;
    }



    /*private void DoSpaceDamage(Entity<VampireComponent> vampire)
    {
        if (!GetAbilityDefinition(vampire.Comp, VampirePowerKey.StellarWeakness, out var def) || def == null)
            return;

        _damageableSystem.TryChangeDamage(vampire, def.Damage, true, origin: vampire);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), vampire, vampire, Shared.Popups.PopupType.LargeCaution);
    }*/
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
}

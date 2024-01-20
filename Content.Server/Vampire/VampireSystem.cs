using Content.Server.Administration.Logs;
using Content.Server.Atmos.Rotting;
using Content.Server.Beam;
using Content.Server.Bed.Sleep;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Components;
using Content.Shared.Store.Events;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Prayer;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Store;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : EntitySystem
{
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
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly MetabolizerSystem _metabolism = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnComponentStartup);

        //SubscribeLocalEvent<VampireComponent, VampireSelfPowerEvent>(OnUseSelfPower);
        //SubscribeLocalEvent<VampireComponent, VampireTargetedPowerEvent>(OnUseTargetedPower);
        SubscribeLocalEvent<VampireComponent, ExaminedEvent>(OnExamined);
        //SubscribeLocalEvent<VampireComponent, StoreProductEvent>(OnStorePurchasePassive);

        SubscribeLocalEvent<VampireHeirloomComponent, UseInHandEvent>(OnUseHeirloom);
        SubscribeLocalEvent<VampireHeirloomComponent, StorePurchasedListingEvent>(OnStorePurchase);

        InitializePowers();
    }

    /*private void OnStorePurchasePassive(EntityUid uid, VampireComponent component, StoreProductEvent args)
    {
        if (args.Ev is not VampirePowerDetails)
            return;

        var def = args.Ev as VampirePowerDetails;
        if (def == null)
            return;

        var vampire = new Entity<VampireComponent>(uid, component);

        switch (def.Type)
        {
            case VampirePowerKey.UnnaturalStrength:
                {
                    UnnaturalStrength(vampire, def);
                    break;
                }
            case VampirePowerKey.SupernaturalStrength:
                {
                    SupernaturalStrength(vampire, def);
                    break;
                }
        }
    }*/

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
                if (!SubtractBloodEssence((uid, vampire), stealth.Upkeep))
                    RemCompDeferred<VampireSealthComponent>(uid);
            }
            stealth.NextStealthTick -= frameTime;
        }

        var healingQuery = EntityQueryEnumerator<VampireComponent, VampireHealingComponent>();
        while (healingQuery.MoveNext(out var uid, out _, out var healing))
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
        //TODO: Popup, deprecation
        if (component.VampireOwner != args.User)
            return;

        //And open the UI
        _store.ToggleUi(args.User, uid);
    }

    private void OnStorePurchase(EntityUid uid, VampireHeirloomComponent component, ref StorePurchasedListingEvent ev)
    {
        if (!TryComp<VampireComponent>(ev.Purchaser, out var vampireComponent))
            return;

        var vampire = new Entity<VampireComponent>(ev.Purchaser, vampireComponent);

        if (ev.Action != null)
        {
            OnStorePurchaseActive(vampire, ev.Action.Value);
        }
        else
        {
            //Its a passive
            OnStorePurchasePassive(vampire, ev.Listing);
        }
    }
    private void OnStorePurchaseActive(Entity<VampireComponent> purchaser, EntityUid purchasedAction)
    {
        if (TryComp<InstantActionComponent>(purchasedAction, out var instantAction) && instantAction.Event != null && instantAction.Event is VampireSelfPowerEvent)
        {
            var vampirePower = instantAction.Event as VampireSelfPowerEvent;
            if (vampirePower == null) return;
            purchaser.Comp.UnlockedPowers[vampirePower.DefinitionName] = purchasedAction;
            return;
        }
        if (TryComp<EntityTargetActionComponent>(purchasedAction, out var targetAction) && targetAction.Event != null && targetAction.Event is VampireTargetedPowerEvent)
        {
            var vampirePower = targetAction.Event as VampireTargetedPowerEvent;
            if (vampirePower == null) return;
            purchaser.Comp.UnlockedPowers[vampirePower.DefinitionName] = purchasedAction;
            return;
        }

        UpdateBloodDisplay(purchaser);
    }

    private void OnStorePurchasePassive(Entity<VampireComponent> purchaser, ListingData purchasedPassive)
    {
        if (!_passiveCache.TryGetValue(purchasedPassive.ID, out var passiveDef))
            return;

        //I am so going to hell for this
        foreach (var compToRemove in passiveDef.CompsToRemove.Values)
            RemComp(purchaser, compToRemove.Component.GetType());

        foreach (var compToAdd in passiveDef.CompsToAdd.Values)
            AddComp(purchaser, compToAdd.Component, true);
    }

    private void OnExamined(EntityUid uid, VampireComponent component, ExaminedEvent args)
    {
        if (HasComp<VampireFangsExtendedComponent>(uid) && args.IsInDetailsRange && !_food.IsMouthBlocked(uid))
            args.AddMarkup($"{Loc.GetString("vampire-fangs-extended-examine")}{Environment.NewLine}");
    }
    private bool AddBloodEssence(Entity<VampireComponent> vampire, FixedPoint2 quantity)
    {
        if (quantity < 0)
            return false;

        vampire.Comp.TotalBloodDrank += quantity.Float();
        vampire.Comp.Balance[VampireComponent.CurrencyProto] += quantity;

        UpdateBloodDisplay(vampire);

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

        return true;
    }
    /// <summary>
    /// Use the charges display on SummonHeirloom to show the remaining blood essence
    /// </summary>
    /// <param name="vampire"></param>
    private void UpdateBloodDisplay(Entity<VampireComponent> vampire)
    {
        //Sanity check, you never know who is going to touch this code
        if (!vampire.Comp.Balance.TryGetValue(VampireComponent.CurrencyProto, out var balance))
            return;

        var chargeDisplay = (int) Math.Round((decimal) balance);
        var summonAction = GetPowerEntity(vampire, VampireComponent.SummonActionPrototype);

        if (summonAction == null)
            return;

        _action.SetCharges(summonAction, chargeDisplay);
    }
    private FixedPoint2 GetBloodEssence(Entity<VampireComponent> vampire)
    {
        if (!vampire.Comp.Balance.TryGetValue(VampireComponent.CurrencyProto, out var val))
            return 0;

        return val;
    }

    private void DoSpaceDamage(Entity<VampireComponent> vampire)
    {
        _damageableSystem.TryChangeDamage(vampire, VampireComponent.SpaceDamage, true, origin: vampire);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), vampire, vampire, PopupType.LargeCaution);
    }
    private bool IsInSpace(EntityUid vampireUid)
    {
        var vampireTransform = Transform(vampireUid);
        var vampirePosition = _transform.GetMapCoordinates(vampireTransform);

        if (!_mapMan.TryFindGridAt(vampirePosition, out _, out var grid))
            return true;

        if (!_mapSystem.TryGetTileRef(vampireUid, grid, vampireTransform.Coordinates, out var tileRef))
            return true;

        return tileRef.Tile.IsEmpty || tileRef.IsSpace();
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
}

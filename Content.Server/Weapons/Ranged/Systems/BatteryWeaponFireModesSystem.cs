using System.Linq;
using Content.Shared.Lock;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, AttemptShootEvent>(OnShootAttempt);
    }

    private void OnExamined(EntityUid uid, BatteryWeaponFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);

        if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));
    }

    private BatteryWeaponFireMode GetMode(BatteryWeaponFireModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }
    
    private void OnShootAttempt(EntityUid uid, BatteryWeaponFireModesComponent component, ref AttemptShootEvent args)
    {
        
        var fireMode = component.FireModes[component.CurrentFireMode];
        
        if (fireMode.Conditions != null)
        {
            var conditionArgs = new FireModeConditionConditionArgs(args.User, uid, fireMode, EntityManager);
            var conditionsMet = fireMode.Conditions.All(condition => condition.Condition(conditionArgs));

            if (!conditionsMet)
            {
                SetFireMode(uid, component, 0, args.User);
            }
        }
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.FireModes.Count < 2)
            return;
        
        if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            return;

        for (var i = 0; i < component.FireModes.Count; i++)
        {
            var fireMode = component.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;
            
            if (fireMode.Conditions != null)
            {
                var conditionArgs = new FireModeConditionConditionArgs(args.User, args.Target, fireMode, EntityManager);
                var conditionsMet = fireMode.Conditions.All(condition => condition.Condition(conditionArgs));

                if (!conditionsMet)
                {
                    if (component.CurrentFireMode == index)
                        SetFireMode(uid, component, 0, args.User);
                    continue;
                }
            }

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = i == component.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (component.FireModes.Count < 2)
            return;

        CycleFireMode(uid, component, args.User);
    }

    private void CycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid user)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        
        var fireMode = component.FireModes[index];
        
        if (fireMode.Conditions != null)
        {
            var conditionArgs = new FireModeConditionConditionArgs(user, uid, fireMode, EntityManager);
            var conditionsMet = fireMode.Conditions.All(condition => condition.Condition(conditionArgs));

            if (!conditionsMet)
            {
                return;
            }
        }
        
        SetFireMode(uid, component, index, user);
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        if (TryGetAmmoProvider(uid, out var ammoProvider) && ammoProvider != null)
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                return;

            if (ammoProvider is ProjectileBatteryAmmoProviderComponent projectileAmmo)
            {
                var oldFireCost = projectileAmmo.FireCost;
                projectileAmmo.Prototype = fireMode.Prototype;
                projectileAmmo.FireCost = fireMode.FireCost;

                float fireCostDiff = (float)fireMode.FireCost / (float)oldFireCost;
                projectileAmmo.Shots = (int)Math.Round(projectileAmmo.Shots / fireCostDiff);
                projectileAmmo.Capacity = (int)Math.Round(projectileAmmo.Capacity / fireCostDiff);
                Dirty(uid, projectileAmmo);
            }
            else if (ammoProvider is HitscanBatteryAmmoProviderComponent hitscanAmmo)
            {
                var oldFireCost = hitscanAmmo.FireCost;
                hitscanAmmo.Prototype = fireMode.Prototype;
                hitscanAmmo.FireCost = fireMode.FireCost;

                float fireCostDiff = (float)fireMode.FireCost / (float)oldFireCost;
                hitscanAmmo.Shots = (int)Math.Round(hitscanAmmo.Shots / fireCostDiff);
                hitscanAmmo.Capacity = (int)Math.Round(hitscanAmmo.Capacity / fireCostDiff);
                Dirty(uid, hitscanAmmo);
            }

            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(uid, ref updateClientAmmoEvent);

            if (fireMode.HeldPrefix != null)
                _item.SetHeldPrefix(uid, fireMode.HeldPrefix);

            if (user != null && TryComp<ActorComponent>(user, out var actor))
                _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, actor.PlayerSession);
        }
    }
    
    private bool TryGetAmmoProvider(EntityUid uid, out object? ammoProvider)
    {
        ammoProvider = null;

        if (TryComp<ProjectileBatteryAmmoProviderComponent>(uid, out var projectileProvider))
        {
            ammoProvider = projectileProvider;
            return true;
        }

        if (TryComp<HitscanBatteryAmmoProviderComponent>(uid, out var hitscanProvider))
        {
            ammoProvider = hitscanProvider;
            return true;
        }

        return false;
    }
}

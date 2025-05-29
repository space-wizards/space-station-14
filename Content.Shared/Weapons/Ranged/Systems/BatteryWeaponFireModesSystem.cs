using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, AttemptShootEvent>(OnShootAttempt);
    }

    private void OnExamined(EntityUid uid, BatteryWeaponFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);

        if (TryGetAmmoProvider(uid, out var ammoProvider) && ammoProvider != null)
        {
            if (ammoProvider is ProjectileBatteryAmmoProviderComponent projectileAmmo)
            {
                if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var projectile))
                    return;

                args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", projectile.Name)));
            }
            else if (ammoProvider is HitscanBatteryAmmoProviderComponent hitscanAmmo)
            {
                if (!_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out var hitscan))
                    return;

                args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", hitscan.Name)));
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

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
            return;

        if (!TryGetAmmoProvider(uid, out var ammoProvider) && ammoProvider == null)
            return;

        for (var i = 0; i < component.FireModes.Count; i++)
        {
            var fireMode = component.FireModes[i];
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

            if (ammoProvider is ProjectileBatteryAmmoProviderComponent projectileAmmo)
            {
                var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);

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
            else if (ammoProvider is HitscanBatteryAmmoProviderComponent hitscanAmmo)
            {
                var entProto = _prototypeManager.Index<HitscanPrototype>(fireMode.Prototype);

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
    }

    private void OnUseInHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, UseInHandEvent args)
    {
		//starlight
        if(args.Handled)
            return;

        args.Handled = true;
		//starlight end
        TryCycleFireMode(uid, component, args.User);
    }

    public void TryCycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid? user = null)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        TrySetFireMode(uid, component, index, user);
    }

    public bool TrySetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, uid))
            return false;

        SetFireMode(uid, component, index, user);

        return true;
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        if (TryGetAmmoProvider(uid, out var ammoProvider) && ammoProvider != null)
        {
            if (ammoProvider is ProjectileBatteryAmmoProviderComponent projectileAmmo)
            {
                if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                    return;

                var oldFireCost = projectileAmmo.FireCost;
                projectileAmmo.Prototype = fireMode.Prototype;
                projectileAmmo.FireCost = fireMode.FireCost;

                float fireCostDiff = (float)fireMode.FireCost / (float)oldFireCost;
                projectileAmmo.Shots = (int)Math.Round(projectileAmmo.Shots / fireCostDiff);
                projectileAmmo.Capacity = (int)Math.Round(projectileAmmo.Capacity / fireCostDiff);
                Dirty(uid, projectileAmmo);

                if (user != null && TryComp<ActorComponent>(user, out var actor))
                    _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, actor.PlayerSession);
            }
            else if (ammoProvider is HitscanBatteryAmmoProviderComponent hitscanAmmo)
            {
                if (!_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out var hitscan))
                    return;

                var oldFireCost = hitscanAmmo.FireCost;
                hitscanAmmo.Prototype = fireMode.Prototype;
                hitscanAmmo.FireCost = fireMode.FireCost;

                float fireCostDiff = (float)fireMode.FireCost / (float)oldFireCost;
                hitscanAmmo.Shots = (int)Math.Round(hitscanAmmo.Shots / fireCostDiff);
                hitscanAmmo.Capacity = (int)Math.Round(hitscanAmmo.Capacity / fireCostDiff);
                Dirty(uid, hitscanAmmo);

                if (user != null && TryComp<ActorComponent>(user, out var actor))
                    _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", hitscan.Name)), uid, actor.PlayerSession);
            }

            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(uid, ref updateClientAmmoEvent);

            if (fireMode.HeldPrefix != null)
                _item.SetHeldPrefix(uid, fireMode.HeldPrefix);
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
}

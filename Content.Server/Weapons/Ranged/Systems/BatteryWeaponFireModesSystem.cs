using System.Linq;
using Content.Shared.Lock;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedBatteryWeaponFireModesSystem _fireModes = default!;

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
                _fireModes.SetFireMode(uid, component, 0, args.User);
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
                var conditionsMet = fireMode.Conditions?.All(condition => condition.Condition(conditionArgs)) ?? true;

                if (!conditionsMet)
                {
                    if (component.CurrentFireMode == index)
                        _fireModes.SetFireMode(uid, component, 0, args.User);
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
                    _fireModes.SetFireMode(uid, component, index, args.User);
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
        
        _fireModes.SetFireMode(uid, component, index, user);
    }
}

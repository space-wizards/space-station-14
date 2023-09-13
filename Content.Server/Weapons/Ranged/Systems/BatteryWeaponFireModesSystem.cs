using Content.Server.Popups;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BatteryWeaponFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        if (component.CurrentFireMode == null)
        {
            SetFireMode(uid, component, component.FireModes.First());
        }

        if (component.CurrentFireMode?.Prototype == null)
            return;

        if (!_prototypeManager.TryIndex<EntityPrototype>(component.CurrentFireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        if (component.CurrentFireMode == null)
        {
            SetFireMode(uid, component, component.FireModes.First());
        }

        foreach (var fireMode in component.FireModes)
        {
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = fireMode == component.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, component, fireMode, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, ActivateInWorldEvent args)
    {
        if (component.FireModes == null || component.FireModes.Count < 2)
            return;

        CycleFireMode(uid, component, args.User);
    }

    private void CycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid user)
    {
        int index = (component.CurrentFireMode != null) ?
            Math.Max(component.FireModes.IndexOf(component.CurrentFireMode), 0) + 1 : 1;

        BatteryWeaponFireMode? fireMode;

        if (index >= component.FireModes.Count)
        {
            fireMode = component.FireModes.FirstOrDefault();
        }

        else
        {
            fireMode = component.FireModes[index];
        }

        SetFireMode(uid, component, fireMode, user);
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, BatteryWeaponFireMode? fireMode, EntityUid? user = null)
    {
        if (fireMode?.Prototype == null)
            return;

        component.CurrentFireMode = fireMode;

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                return;

            projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;

            if (user != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
            }
        }
    }
}

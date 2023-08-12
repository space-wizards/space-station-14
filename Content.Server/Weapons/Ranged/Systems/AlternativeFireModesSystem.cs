using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class AlternativeFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlternativeFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<AlternativeFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<AlternativeFireModesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, AlternativeFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        if (component.CurrentFireMode?.Prototype == null)
            return;

        _prototypeManager.TryIndex(component.CurrentFireMode.Prototype, out EntityPrototype? proto);

        if (proto == null)
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));
    }

    private void OnGetVerb(EntityUid uid, AlternativeFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.FireModes.Count < 2)
            return;

        foreach (var fireMode in component.FireModes)
        {
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = fireMode == component.CurrentFireMode,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    component.CurrentFireModeIndex = component.FireModes.IndexOf(component.FireModes.First(x => x.Prototype == fireMode.Prototype));
                    SetFireMode(uid, component, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, AlternativeFireModesComponent component, ActivateInWorldEvent args)
    {
        if (component.FireModes == null || !component.FireModes.Any())
            return;

        component.CurrentFireModeIndex++;
        SetFireMode(uid, component, args.User);
    }

    private void SetFireMode(EntityUid uid, AlternativeFireModesComponent component, EntityUid user)
    {
        var proto = component.CurrentFireMode?.Prototype;
        var fireCost = component.CurrentFireMode?.FireCost;

        if (proto == null || fireCost == null)
            return;

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(proto, out var prototype))
                return;

            projectileBatteryAmmoProvider.Prototype = (string)proto;
            projectileBatteryAmmoProvider.FireCost = (float)fireCost;

            _popupSystem.PopupEntity(Loc.GetString("gun-selected-mode", ("mode", prototype.Name)), uid, user);
        }
    }
}

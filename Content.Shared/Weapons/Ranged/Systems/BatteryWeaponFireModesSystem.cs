using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {

        base.Initialize();
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<BatteryWeaponFireModesComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var fireMode = GetMode(ent.Comp);

        if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", proto.Name)));
    }

    private BatteryWeaponFireMode GetMode(BatteryWeaponFireModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(Entity<BatteryWeaponFireModesComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (ent.Comp.FireModes.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, ent))
            return;

        var target = args.User;

        for (var i = 0; i < ent.Comp.FireModes.Count; i++)
        {
            var fireMode = ent.Comp.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = i == ent.Comp.CurrentFireMode,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetFireMode(ent, index, target);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnUseInHandEvent(Entity<BatteryWeaponFireModesComponent> ent, ref UseInHandEvent args)
    {
        if(args.Handled)
            return;

        args.Handled = true;
        TryCycleFireMode(ent, args.User);
    }

    public void TryCycleFireMode(Entity<BatteryWeaponFireModesComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var index = (ent.Comp.CurrentFireMode + 1) % ent.Comp.FireModes.Count;
        TrySetFireMode(ent, index, user);
    }

    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent> ent, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= ent.Comp.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, ent))
            return false;

        SetFireMode(ent, index, user);

        return true;
    }

    private void SetFireMode(Entity<BatteryWeaponFireModesComponent> ent, int index, EntityUid? user = null)
    {
        var fireMode = ent.Comp.FireModes[index];
        ent.Comp.CurrentFireMode = index;
        Dirty(ent);

        if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        {
            if (TryComp<AppearanceComponent>(ent, out var appearance))
            {
                _appearanceSystem.SetData(ent, BatteryWeaponFireModeVisuals.State, prototype.ID, appearance);
                _appearanceSystem.SetData(ent, BatteryWeaponFireModeVisualizer.Color, fireMode.Color, appearance);
            }

            if (user != null)
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), ent, user.Value);
        }

        if (TryComp(ent, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProviderComponent))
        {
            // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
            var oldFireCost = projectileBatteryAmmoProviderComponent.FireCost;
            projectileBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            var fireCostDiff = fireMode.FireCost / oldFireCost;
            projectileBatteryAmmoProviderComponent.Shots = (int)Math.Round(projectileBatteryAmmoProviderComponent.Shots / fireCostDiff);
            projectileBatteryAmmoProviderComponent.Capacity = (int)Math.Round(projectileBatteryAmmoProviderComponent.Capacity / fireCostDiff);

            Dirty(ent, projectileBatteryAmmoProviderComponent);

            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(ent, ref updateClientAmmoEvent);
        }
    }

    /// <summary>
    /// Initialize the appearance and firemode.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnComponentInit(Entity<BatteryWeaponFireModesComponent> ent, ref ComponentInit args)
    {
        SetFireMode(ent, 0);
    }
}

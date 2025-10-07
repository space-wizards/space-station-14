using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, BatteryWeaponFireModeChangeMessage>(OnModeSet);
    }

    private void OnModeSet(EntityUid uid, BatteryWeaponFireModesComponent component, BatteryWeaponFireModeChangeMessage args)
    {
        TrySetFireMode(uid, component, args.ModeIndex, args.Actor);
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

    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent?> ent, int index, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return TrySetFireMode(ent, ent.Comp, index, user);
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

    public bool TrySetFireMode(
        EntityUid uid,
        BatteryWeaponFireModesComponent component,
        EntProtoId protoId,
        EntityUid? user = null
    )
    {
        foreach (var mode in component.FireModes)
        {
            if (mode.Prototype == protoId)
            {
                SetFireMode(uid, component, mode, user);
                return true;
            }
        }

        return false;
    }

    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent?> ent, EntProtoId protoId, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return TrySetFireMode(ent, ent.Comp, protoId, user);
    }

    private void SetFireMode(
        EntityUid uid,
        BatteryWeaponFireModesComponent component,
        int index,
        EntityUid? user = null
    )
    {
        var fireMode = component.FireModes[index];

        SetFireMode(uid, component, fireMode, user);
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, BatteryWeaponFireMode fireMode, EntityUid? user = null)
    {
        component.CurrentFireMode = component.FireModes.IndexOf(fireMode);
        Dirty(uid, component);

        if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearanceSystem.SetData(uid, BatteryWeaponFireModeVisuals.State, prototype.ID, appearance);

            if (user != null)
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
        }

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProviderComponent))
        {
            // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
            projectileBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            var oldFireCost = projectileBatteryAmmoProviderComponent.FireCost;
            float fireCostDiff = fireMode.FireCost / oldFireCost;
            projectileBatteryAmmoProviderComponent.Shots = (int)Math.Round(projectileBatteryAmmoProviderComponent.Shots / fireCostDiff);
            projectileBatteryAmmoProviderComponent.Capacity = (int)Math.Round(projectileBatteryAmmoProviderComponent.Capacity / fireCostDiff);

            Dirty(uid, projectileBatteryAmmoProviderComponent);

            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(uid, ref updateClientAmmoEvent);
        }
    }

    public bool TryGetFireMode(Entity<BatteryWeaponFireModesComponent?> ent, [NotNullWhen(true)] out BatteryWeaponFireMode? fireMode)
    {
        fireMode = null;
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        var fireModeIndex = ent.Comp.CurrentFireMode;
        if (fireModeIndex < 0 || fireModeIndex >= ent.Comp.FireModes.Count)
        {
            Log.Warning(
                $"Current fire mode is in unexpected state - current index is '{fireModeIndex}' "
                + $"while fireModes contain '{ent.Comp.FireModes.Count}' elements."
            );
            return false;
        }

        fireMode = ent.Comp.FireModes[fireModeIndex];
        return true;
    }
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModesUiKey
{
    Key
}


using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

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

    private void OnExamined(Entity<BatteryWeaponFireModesComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var fireMode = GetMode(ent.Comp);

        if (!ProtoMan.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode-examine", ("mode", proto.Name)));
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

        if (ProtoMan.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearanceSystem.SetData(uid, BatteryWeaponFireModeVisuals.State, prototype.ID, appearance);

            if (user != null)
                _popup.PopupEntity(Loc.GetString("gun-set-fire-mode-popup", ("mode", prototype.Name)), uid, user.Value);
        }

        if (TryComp(uid, out BatteryAmmoProviderComponent? batteryAmmoProviderComponent))
        {
            // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
            batteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            batteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            var oldFireCost = batteryAmmoProviderComponent.FireCost;
            float fireCostDiff = fireMode.FireCost / oldFireCost;
            batteryAmmoProviderComponent.Shots = (int)Math.Round(batteryAmmoProviderComponent.Shots / fireCostDiff);
            batteryAmmoProviderComponent.Capacity = (int)Math.Round(batteryAmmoProviderComponent.Capacity / fireCostDiff);

            Dirty(uid, batteryAmmoProviderComponent);

            _gun.UpdateShots((uid, batteryAmmoProviderComponent));
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


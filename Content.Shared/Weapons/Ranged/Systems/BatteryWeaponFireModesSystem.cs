using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerStateSystem _powerState = default!;

    /// <summary> Applies request for setting fire mode. </summary>
    [SubscribeLocalEvent]
    private void OnModeSet(Entity<BatteryWeaponFireModesComponent> ent, ref BatteryWeaponFireModeChangeMessage args)
    {
        TrySetFireMode(ent, ent, args.ModeIndex, args.Actor);
    }

    /// <summary> Adds current fire mode to examine data, if there are more then 2. </summary>
    [SubscribeLocalEvent]
    private void OnExamined(Entity<BatteryWeaponFireModesComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var fireMode = ent.Comp.FireModes[ent.Comp.CurrentFireMode];

        if (!ProtoMan.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode-examine", ("mode", proto.Name)));
    }

    /// <summary> Sets fire mode if provided signalr port is withing <see cref="BatteryWeaponFireModesComponent.SetTypePorts"/>. </summary>
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<BatteryWeaponFireModesComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.SetTypePorts.TryGetValue(args.Port, out var protoId))
            TrySetFireMode((ent.Owner, null), protoId);
    }

    /// <summary>
    /// Sets fire mode to next one by index.
    /// </summary>
    /// <param name="ent">Entity with fire modes.</param>
    /// <param name="user"> User that requested changing fire mode. </param>
    [PublicAPI]
    public void TryCycleFireMode(Entity<BatteryWeaponFireModesComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var index = (ent.Comp.CurrentFireMode + 1) % ent.Comp.FireModes.Count;
        TrySetFireMode(ent.AsNullable(), index, user);
    }

    /// <summary>
    /// Attempts to 
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="index"></param>
    /// <param name="user"> User that requested changing fire mode. </param>
    /// <returns></returns>
    [PublicAPI]
    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent?> ent, int index, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return TrySetFireMode(ent, ent.Comp, index, user);
    }

    /// <summary>
    /// Sets fire mode to the one with provided index in <see cref="BatteryWeaponFireModesComponent.FireModes"/> list if it is possible.
    /// </summary>
    /// <param name="uid"> Device on which fire mode change was requested. </param>
    /// <param name="component"> Fire mode comp. </param>
    /// <param name="index"> Index of requested fire mode in <see cref="BatteryWeaponFireModesComponent.FireModes"/> list. </param>
    /// <param name="user"> User that requested changing fire mode. </param>
    /// <returns>
    /// True if fire mode change was successful, false if it was not (for example, due to provided index being out of range).
    /// </returns>
    [PublicAPI]
    public bool TrySetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.FireModes.Count)
            return false;

        SetFireMode(uid, component, index, user);

        return true;
    }

    /// <summary>
    /// Sets fire mode to the one with provided prototype, if it is in <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
    /// </summary>
    /// <param name="uid"> Device on which fire mode change was requested. </param>
    /// <param name="component"> Fire mode comp. </param>
    /// <param name="protoId">
    /// ProtoId for desired projectile, should be in one of fire modes
    /// listed in <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
    /// </param>
    /// <param name="user"> User that requested changing fire mode. </param>
    /// <returns>
    /// True if fire mode change was successful, false if it was not
    /// (for example, due to provided prototype not being mentioned in <see cref="BatteryWeaponFireModesComponent.FireModes"/>).
    /// </returns>
    [PublicAPI]
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

    /// <summary>
    /// Sets fire mode to the one with provided prototype, if it is in <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
    /// </summary>
    /// <param name="ent"> Device on which fire mode change was requested. </param>
    /// <param name="protoId">
    /// ProtoId for desired projectile, should be in one of fire modes
    /// listed in <see cref="BatteryWeaponFireModesComponent.FireModes"/>.
    /// </param>
    /// <param name="user"> User that requested changing fire mode. </param>
    /// <returns></returns>
    [PublicAPI]
    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent?> ent, EntProtoId protoId, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return TrySetFireMode(ent, ent.Comp, protoId, user);
    }

    /// <summary>
    /// Attempts to get current fire mode of device.
    /// </summary>
    /// <param name="ent"> Device with fire modes. </param>
    /// <param name="fireMode">
    /// Current fire mode of device, or null if not set,
    /// or no <see cref="BatteryWeaponFireModesComponent"/> is placed on entity.
    /// </param>
    /// <returns> True if fire mode was extracted successfully, false otherwise. </returns>
    [PublicAPI]
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

        if (TryComp(uid, out NetworkPoweredAmmoProviderComponent? networkAmmoProviderComponent))
        {
            networkAmmoProviderComponent.Prototype = fireMode.Prototype;
            Dirty(uid, networkAmmoProviderComponent);
        }

        if (TryComp(uid, out PowerStateComponent? powerStateComponent))
        {
            _powerState.SetWorkingPowerDraw((uid, powerStateComponent), fireMode.FireCost);
        }
    }
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModesUiKey
{
    Key
}


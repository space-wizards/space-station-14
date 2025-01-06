using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class SharedBatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    
    public void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProviderComponent))
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
                return;

            // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
            var OldFireCost = projectileBatteryAmmoProviderComponent.FireCost;
            projectileBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            projectileBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;
            float FireCostDiff = (float)fireMode.FireCost / (float)OldFireCost;
            projectileBatteryAmmoProviderComponent.Shots = (int)Math.Round(projectileBatteryAmmoProviderComponent.Shots/FireCostDiff);
            projectileBatteryAmmoProviderComponent.Capacity = (int)Math.Round(projectileBatteryAmmoProviderComponent.Capacity/FireCostDiff);
            Dirty(uid, projectileBatteryAmmoProviderComponent);
            var updateClientAmmoEvent = new UpdateClientAmmoEvent();
            RaiseLocalEvent(uid, ref updateClientAmmoEvent);
            
            if (fireMode.HeldPrefix != null)
                _item.SetHeldPrefix(uid, fireMode.HeldPrefix);
            
            var fireModeChangedEvent = new FireModeChangedEvent();
            RaiseLocalEvent(uid, ref fireModeChangedEvent);

            if (user != null)
            {
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
            }
        }
    }
}
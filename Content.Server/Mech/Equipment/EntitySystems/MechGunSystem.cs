using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>((id, cmp, _) => TryChargeGunBattery(id, cmp));
        SubscribeLocalEvent<MechEquipmentComponent, OnEmptyGunShotEvent>((id, cmp, _) => TryChargeGunBattery(id, cmp));
    }

    private void TryChargeGunBattery(EntityUid uid, MechEquipmentComponent component)
    {
        if (component.EquipmentOwner.HasValue
            && HasComp<MechComponent>(component.EquipmentOwner.Value)
            && TryComp<BatteryComponent>(uid, out var battery))
            ChargeGunBattery(uid, battery);
    }

    private void ChargeGunBattery(EntityUid uid, BatteryComponent component)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var mechEquipment)
            || !mechEquipment.EquipmentOwner.HasValue
            || !TryComp<MechComponent>(mechEquipment.EquipmentOwner.Value, out var mech))
            return;

        var chargeDelta = component.MaxCharge - component.CurrentCharge;
        // TODO: The battery charge of the mech would be spent directly when fired.
        if (chargeDelta <= 0 
            || mech.Energy - chargeDelta < 0
            || !_mech.TryChangeEnergy(mechEquipment.EquipmentOwner.Value, -chargeDelta, mech))
            return;

        _battery.SetCharge(uid, component.MaxCharge, component);
    }
}
using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(MechGunShot);
    }

    private void MechGunShot(EntityUid uid, MechEquipmentComponent component, ref GunShotEvent args)
    {
        if (!component.EquipmentOwner.HasValue)
            return;

        if (!TryComp<MechComponent>(component.EquipmentOwner.Value, out var mech))
            return;

        if (TryComp<BatteryComponent>(uid, out var battery))
        {
            ChargeGunBattery(uid, battery);
            return;
        }
    }

    private void ChargeGunBattery(EntityUid uid, BatteryComponent component)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var mechEquipment) || !mechEquipment.EquipmentOwner.HasValue)
            return;

        if (!TryComp<MechComponent>(mechEquipment.EquipmentOwner.Value, out var mech))
            return;

        var maxCharge = component.MaxCharge;
        var currentCharge = component.CurrentCharge;

        var chargeDelta = maxCharge - currentCharge;

        // TODO: The battery charge of the mech would be spent directly when fired.
        if (chargeDelta <= 0 || mech.Energy - chargeDelta < 0)
            return;

        if (!_mech.TryChangeEnergy(mechEquipment.EquipmentOwner.Value, -chargeDelta, mech))
            return;

        _battery.SetCharge(uid, component.MaxCharge, component);
    }
}
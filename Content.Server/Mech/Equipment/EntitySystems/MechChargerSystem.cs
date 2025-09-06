using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Mech.Components;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechChargerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent, ChargerComponent>();
        while (query.MoveNext(out var mechUid, out var mech, out var charger))
        {
            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBatteryEnt, out var mechBattery, null))
                continue;

            if (mechBatteryEnt == null || mechBattery.CurrentCharge <= 0)
                continue;

            var chargeRate = charger.ChargeRate;
            if (chargeRate <= 0f)
                continue;

            // Get the container to charge
            var container = _containers.TryGetContainer(mechUid, charger.SlotId, out var cont)
                ? cont
                : mech.EquipmentContainer;

            // Charge all weapons in the container
            foreach (var weapon in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(charger.Whitelist, weapon))
                    continue;

                if (!TryComp<BatteryComponent>(weapon, out var battery))
                    continue;

                if (battery.CurrentCharge >= battery.MaxCharge)
                    continue;

                var chargeNeeded = battery.MaxCharge - battery.CurrentCharge;
                var chargeAvailable = mechBattery.CurrentCharge;
                var chargeToAdd = MathF.Min(MathF.Min(chargeRate, chargeNeeded), chargeAvailable);

                if (chargeToAdd <= 0)
                    continue;

                if (_battery.TryUseCharge(mechBatteryEnt.Value, chargeToAdd, mechBattery))
                    _battery.SetCharge(weapon, battery.CurrentCharge + chargeToAdd, battery);
            }
        }
    }
}

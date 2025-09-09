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
    private readonly Dictionary<EntityUid, float> _weaponEnergyBuffer = new();

    /// TODO: need a ChargerSystem.cs refractor so it can charge batteries from another battery in the slot.
    /// To avoid most of this crap.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent, ChargerComponent>();
        while (query.MoveNext(out var mechUid, out var mech, out var charger))
        {
            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBatteryEnt, out var mechBattery, null))
                continue;

            if (mechBattery.CurrentCharge <= 0)
                continue;

            if (charger.ChargeRate <= 0f)
                continue;

            // Get the container to charge.
            var container = _containers.TryGetContainer(mechUid, charger.SlotId, out var cont)
                ? cont
                : mech.EquipmentContainer;

            // Charge all weapons in the container.
            foreach (var weapon in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(charger.Whitelist, weapon))
                    continue;

                if (!TryComp<BatteryComponent>(weapon, out var battery))
                    continue;

                if (battery.CurrentCharge >= battery.MaxCharge)
                    continue;

                var chargeNeeded = battery.MaxCharge - battery.CurrentCharge;

                // Reserve additional energy from mech battery into a per-weapon buffer.
                _weaponEnergyBuffer.TryGetValue(weapon, out var buffered);
                var additionalNeeded = chargeNeeded - buffered;
                if (additionalNeeded > 0f)
                {
                    var toReserve = MathF.Min(additionalNeeded, mechBattery.CurrentCharge);
                    if (toReserve > 0f && _battery.TryUseCharge(mechBatteryEnt.Value, toReserve, mechBattery))
                    {
                        buffered += toReserve;
                        _weaponEnergyBuffer[weapon] = buffered;
                    }
                }

                // Transfer from buffer to weapon at the normal charge rate (scaled by frame time).
                var transfer = MathF.Min(charger.ChargeRate, MathF.Min(buffered, chargeNeeded));
                if (transfer > 0f)
                {
                    var newCharge = battery.CurrentCharge + transfer;
                    _battery.SetCharge(weapon, newCharge, battery);
                    buffered -= transfer;

                    if (buffered <= 0f || newCharge >= battery.MaxCharge)
                        _weaponEnergyBuffer.Remove(weapon);
                    else
                        _weaponEnergyBuffer[weapon] = buffered;
                }
            }
        }
    }
}

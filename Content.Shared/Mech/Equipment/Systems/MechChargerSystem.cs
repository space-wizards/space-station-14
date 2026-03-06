using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Mech.Equipment.Systems;

/// <summary>
/// Charges equipment batteries inside mechs using the mech's own power cell.
/// </summary>
public sealed class MechChargerSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private readonly Dictionary<EntityUid, float> _equipmentEnergyBuffer = [];

    /// <inheritdoc/>
    /// TODO: need a ChargerSystem refractor so it can charge batteries from another battery in the slot.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent, ChargerComponent>();
        while (query.MoveNext(out var mechUid, out var mech, out var charger))
        {
            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBattery))
                continue;

            var mechCharge = _battery.GetCharge(mechBattery.Value.AsNullable());
            if (mechCharge <= 0)
                continue;

            if (charger.ChargeRate <= 0f)
                continue;

            // Get the container to charge.
            var container = _containers.TryGetContainer(mechUid, charger.SlotId, out var cont)
                ? cont
                : mech.EquipmentContainer;

            // Charge all weapons in the container.
            foreach (var equipment in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(charger.Whitelist, equipment))
                    continue;

                if (!TryComp<BatteryComponent>(equipment, out var equipmentBattery))
                    continue;

                var equipmentCharge = _battery.GetCharge((equipment, equipmentBattery));
                if (equipmentCharge >= equipmentBattery.MaxCharge)
                    continue;

                var chargeNeeded = equipmentBattery.MaxCharge - equipmentCharge;

                // Reserve additional energy from mech battery into a per-equipment buffer.
                _equipmentEnergyBuffer.TryGetValue(equipment, out var buffered);
                var additionalNeeded = chargeNeeded - buffered;
                if (additionalNeeded > 0f)
                {
                    var toReserve = MathF.Min(additionalNeeded, mechCharge);
                    if (toReserve > 0f && _battery.TryUseCharge(mechBattery.Value.AsNullable(), toReserve))
                    {
                        buffered += toReserve;
                        _equipmentEnergyBuffer[equipment] = buffered;
                    }
                }

                // Transfer from buffer to equipment at the normal charge rate (scaled by frame time).
                var transfer = MathF.Min(charger.ChargeRate, MathF.Min(buffered, chargeNeeded));
                if (transfer > 0f)
                {
                    var newCharge = equipmentCharge + transfer;
                    _battery.SetCharge((equipment, equipmentBattery), newCharge);
                    buffered -= transfer;

                    if (buffered <= 0f || newCharge >= equipmentBattery.MaxCharge)
                        _equipmentEnergyBuffer.Remove(equipment);
                    else
                        _equipmentEnergyBuffer[equipment] = buffered;
                }
            }
        }
    }
}

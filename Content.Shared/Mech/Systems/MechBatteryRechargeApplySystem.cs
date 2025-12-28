using Content.Shared.Mech.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Applies the sum of recharge rates accumulated on a mech during the current tick to the mech's battery
/// by enabling <see cref="BatterySelfRechargerComponent"/> at the computed rate, then clears the accumulator.
/// </summary>
public sealed class MechBatteryRechargeApplySystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MechComponent, MechEnergyAccumulatorComponent>();
        while (query.MoveNext(out var mechUid, out var _, out var acc))
        {
            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBattery))
            {
                acc.PendingRechargeRate = 0f;
                continue;
            }

            var total = acc.PendingRechargeRate;
            acc.PendingRechargeRate = 0f;

            var self = EnsureComp<BatterySelfRechargerComponent>(mechBattery.Value);
            if (!MathHelper.CloseTo(self.AutoRechargeRate, total))
            {
                self.AutoRechargeRate = total;
                Dirty(mechBattery.Value, self);
                _battery.RefreshChargeRate((mechBattery.Value, mechBattery.Value));
            }
        }
    }
}

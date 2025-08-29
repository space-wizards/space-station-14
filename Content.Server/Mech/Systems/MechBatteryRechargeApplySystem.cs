using Content.Server.Mech.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Mech.Components;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Applies the sum of recharge rates accumulated on a mech during the current tick to the mech's battery
/// by enabling BatterySelfRecharger at the computed rate, then clears the accumulator.
/// </summary>
public sealed partial class MechBatteryRechargeApplySystem : EntitySystem
{
	[Dependency] private readonly PowerCellSystem _powerCell = default!;

	public override void Update(float frameTime)
	{
		var query = EntityQueryEnumerator<MechComponent, MechEnergyAccumulatorComponent>();
		while (query.MoveNext(out var mechUid, out var mech, out var acc))
		{
			if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBatteryEnt, out var mechBattery, null))
			{
				acc.PendingRechargeRate = 0f;
				continue;
			}

			var total = acc.PendingRechargeRate;
			acc.PendingRechargeRate = 0f;

			var self = EnsureComp<BatterySelfRechargerComponent>(mechBatteryEnt.Value);
			var newAuto = total > 0f;
			var newRate = newAuto ? total : 0f;
			if (self.AutoRecharge != newAuto || !MathHelper.CloseTo(self.AutoRechargeRate, newRate))
			{
				self.AutoRecharge = newAuto;
				self.AutoRechargeRate = newRate;
			}
		}
	}
}

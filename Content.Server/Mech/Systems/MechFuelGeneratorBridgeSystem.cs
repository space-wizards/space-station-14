using Content.Server.Mech.Components;
using Content.Server.Power.Generator;
using Content.Shared.Mech.Components;
using Content.Shared.Power.Generator;
using Robust.Shared.GameObjects;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Bridges mech FuelGenerator-based modules to the mech battery by consuming fuel via the standard
/// generator events and adding the module's chargeRate into the per-tick recharge accumulator.
/// </summary>
public sealed partial class MechFuelGeneratorBridgeSystem : EntitySystem
{
	public override void Update(float frameTime)
	{
		var query = EntityQueryEnumerator<MechComponent>();
		while (query.MoveNext(out var mechUid, out var mech))
		{
			if (!TryComp<MechEnergyAccumulatorComponent>(mechUid, out var acc))
				acc = EnsureComp<MechEnergyAccumulatorComponent>(mechUid);

			foreach (var module in mech.ModuleContainer.ContainedEntities)
			{
				if (!TryComp<MechGeneratorModuleComponent>(module, out var gen))
					continue;
				if (gen.GenerationType != MechGenerationType.FuelGenerator)
					continue;

				var telem = EnsureComp<MechEnergyAccumulatorComponent>(module);
				telem.Max = 0f;
				telem.Current = 0f;

				var getFuel = new GeneratorGetFuelEvent(default);
				RaiseLocalEvent(module, ref getFuel);

				if (!TryComp<FuelGeneratorComponent>(module, out var fuelGen))
					continue;

				// max output is the configured target power
				telem.Max = fuelGen.TargetPower;

				if (getFuel.Fuel <= 0)
					continue;

				var eff = 1 / SharedGeneratorSystem.CalcFuelEfficiency(fuelGen.TargetPower, fuelGen.OptimalPower, fuelGen);
				var burn = fuelGen.OptimalBurnRate * frameTime * eff;
				RaiseLocalEvent(module, new GeneratorUseFuel(burn));

				// Current contribution equals target power when fuel is available
				var current = fuelGen.TargetPower;
				acc.PendingRechargeRate += current;
				telem.Current = current;
			}
		}
	}
}
